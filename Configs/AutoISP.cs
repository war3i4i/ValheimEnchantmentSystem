using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
namespace System.Runtime.CompilerServices { [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)] public sealed class ModuleInitializerAttribute : Attribute { } }

namespace ISP_Auto
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class AutoSerialize : Attribute;

    internal static class ISP_Patcher
    {
        private static Dictionary<Type, List<ISP_Field>> FieldsByType = new();
        private class ISP_Field { public Action<object, ZPackage> Serialize; public Action<object, ZPackage> Deserialize; }

        [ModuleInitializer]
        internal static void Init()
        {
            List<Type> typesToPatch = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<AutoSerialize>() != null && t.GetInterface(nameof(ISerializableParameter)) != null).ToList();
            foreach (Type type in typesToPatch)
            {
                if (type.IsValueType) continue;
                FieldsByType[type] = new();
                List<FieldInfo> fields = AccessTools.GetDeclaredFields(type).Where(f => f.GetCustomAttribute<SerializeField>() != null).ToList();
                foreach (FieldInfo field in fields)
                {
                    ISP_Field info = new ISP_Field();
                    info.Serialize = GetSerializeDelegate(field);
                    if (info.Serialize == null) { ZLog.LogError($"Error creating Serialize delegate for {field}. Type is not supported"); continue; }
                    info.Deserialize = GetDeserializeDelegate(field);
                    if (info.Deserialize == null) { ZLog.LogError($"Error creating Deserialize delegate for {field}. Type is not supported"); continue; }
                    FieldsByType[type].Add(info);
                }
            }

            Harmony harmony = new Harmony("AutoISP_" + Assembly.GetExecutingAssembly().GetName().Name);
            HarmonyMethod serializeTranspiler = new(AccessTools.Method(typeof(ISP_Patcher), nameof(Transpiler_Serialize)));
            HarmonyMethod deserializeTranspiler = new(AccessTools.Method(typeof(ISP_Patcher), nameof(Transpiler_Deserialize)));
            foreach (Type type in typesToPatch)
            {
                MethodInfo serializeMethod = AccessTools.Method(type, nameof(ISerializableParameter.Serialize));
                MethodInfo deserializeMethod = AccessTools.Method(type, nameof(ISerializableParameter.Deserialize));
                if (serializeMethod == null || deserializeMethod == null) { ZLog.LogError($"Error: {type.Name} does not implement ISerializableParameter"); continue; }
                harmony.Patch(serializeMethod, transpiler: serializeTranspiler);
                harmony.Patch(deserializeMethod, transpiler: deserializeTranspiler);
            }
        }
        
        #region HarmonyMethods
        private static void Replace_Serialize(object instance, ref ZPackage pkg) { Type t = instance.GetType(); if (!FieldsByType.ContainsKey(t)) return; foreach (ISP_Field field in FieldsByType[t]) field.Serialize(instance, pkg); }
        private static void Replace_Deserialize(object instance, ref ZPackage pkg) { Type t = instance.GetType(); if (!FieldsByType.ContainsKey(t)) return; foreach (ISP_Field field in FieldsByType[t]) field.Deserialize(instance, pkg); }
        private static CodeInstruction[] TranspileWithMethod(MethodInfo method) => new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Call, method), new CodeInstruction(OpCodes.Ret) };
        private static IEnumerable<CodeInstruction> Transpiler_Serialize(IEnumerable<CodeInstruction> instructions) => TranspileWithMethod(AccessTools.Method(typeof(ISP_Patcher), nameof(Replace_Serialize)));
        private static IEnumerable<CodeInstruction> Transpiler_Deserialize(IEnumerable<CodeInstruction> instructions) => TranspileWithMethod(AccessTools.Method(typeof(ISP_Patcher), nameof(Replace_Deserialize)));
        #endregion
        
        #region SerializeDelegates
        private static Action<object, ZPackage> GetSerializeDelegate(FieldInfo field)
        {
            Type t = field.FieldType;
            if (t.IsEnum) return ((object instance, ZPackage pkg) => SerializeInt(pkg, field.GetValue(instance)));
            if (t.GetInterface(nameof(ISerializableParameter)) != null) return ((object instance, ZPackage pkg) => SerializeISP(pkg, field.GetValue(instance)));

            bool isList = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);
            if (isList)
            {
                Type listInner = t.GetGenericArguments()[0];
                if (listInner.GetInterface(nameof(ISerializableParameter)) != null) return ((object instance, ZPackage pkg) => SerializeList_ISP(pkg, (IList)field.GetValue(instance)));

                if (listInner == typeof(int)) return ((object instance, ZPackage pkg) => SerializeList_Int(pkg, (List<int>)field.GetValue(instance)));
                if (listInner == typeof(uint)) return ((object instance, ZPackage pkg) => SerializeList_UInt(pkg, (List<uint>)field.GetValue(instance)));
                if (listInner == typeof(long)) return ((object instance, ZPackage pkg) => SerializeList_Long(pkg, (List<long>)field.GetValue(instance)));
                if (listInner == typeof(float)) return ((object instance, ZPackage pkg) => SerializeList_Float(pkg, (List<float>)field.GetValue(instance)));
                if (listInner == typeof(double)) return ((object instance, ZPackage pkg) => SerializeList_Double(pkg, (List<double>)field.GetValue(instance)));
                if (listInner == typeof(bool)) return ((object instance, ZPackage pkg) => SerializeList_Bool(pkg, (List<bool>)field.GetValue(instance)));
                if (listInner == typeof(string)) return ((object instance, ZPackage pkg) => SerializeList_String(pkg, (List<string>)field.GetValue(instance)));
                if (listInner == typeof(Quaternion)) return ((object instance, ZPackage pkg) => SerializeList_Quaternion(pkg, (List<Quaternion>)field.GetValue(instance)));
                if (listInner == typeof(Vector2i)) return ((object instance, ZPackage pkg) => SerializeList_Vector2i(pkg, (List<Vector2i>)field.GetValue(instance)));
                if (listInner == typeof(Vector3)) return ((object instance, ZPackage pkg) => SerializeList_Vector(pkg, (List<Vector3>)field.GetValue(instance)));
                if (listInner == typeof(Color)) return ((object instance, ZPackage pkg) => SerializeList_Color(pkg, (List<Color>)field.GetValue(instance)));
                if (listInner == typeof(Color32)) return ((object instance, ZPackage pkg) => SerializeList_Color32(pkg, (List<Color32>)field.GetValue(instance)));
                if (listInner == typeof(ZPackage)) return ((object instance, ZPackage pkg) => SerializeList_ZPackage(pkg, (List<ZPackage>)field.GetValue(instance)));
            }

            bool isDictionary = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
            if (isDictionary)
            {
                Type dictKey = t.GetGenericArguments()[0];
                if (dictKey != typeof(int) && dictKey != typeof(string))
                {
                    ZLog.LogError($"Error: Dictionary key type {dictKey} is not supported");
                    return null;
                }

                Type dictValue = t.GetGenericArguments()[1];
                if (dictValue.GetInterface(nameof(ISerializableParameter)) != null)
                {
                    if (dictKey == typeof(int)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_ISP(pkg, (IDictionary)field.GetValue(instance)));
                    if (dictKey == typeof(string)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_ISP(pkg, (IDictionary)field.GetValue(instance)));
                }

                if (dictKey == typeof(int))
                {
                    if (dictValue == typeof(int)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Int(pkg, (Dictionary<int, int>)field.GetValue(instance)));
                    if (dictValue == typeof(uint)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_UInt(pkg, (Dictionary<int, uint>)field.GetValue(instance)));
                    if (dictValue == typeof(long)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Long(pkg, (Dictionary<int, long>)field.GetValue(instance)));
                    if (dictValue == typeof(float)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Float(pkg, (Dictionary<int, float>)field.GetValue(instance)));
                    if (dictValue == typeof(double)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Double(pkg, (Dictionary<int, double>)field.GetValue(instance)));
                    if (dictValue == typeof(bool)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Bool(pkg, (Dictionary<int, bool>)field.GetValue(instance)));
                    if (dictValue == typeof(string)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_String(pkg, (Dictionary<int, string>)field.GetValue(instance)));
                    if (dictValue == typeof(Quaternion)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Quaternion(pkg, (Dictionary<int, Quaternion>)field.GetValue(instance)));
                    if (dictValue == typeof(Vector2i)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Vector2i(pkg, (Dictionary<int, Vector2i>)field.GetValue(instance)));
                    if (dictValue == typeof(Vector3)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Vector(pkg, (Dictionary<int, Vector3>)field.GetValue(instance)));
                    if (dictValue == typeof(Color)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Color(pkg, (Dictionary<int, Color>)field.GetValue(instance)));
                    if (dictValue == typeof(Color32)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_Color32(pkg, (Dictionary<int, Color32>)field.GetValue(instance)));
                    if (dictValue == typeof(ZPackage)) return ((object instance, ZPackage pkg) => SerializeDictionary_Int_ZPackage(pkg, (Dictionary<int, ZPackage>)field.GetValue(instance)));
                }

                if (dictKey == typeof(string))
                {
                    if (dictValue == typeof(int)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Int(pkg, (Dictionary<string, int>)field.GetValue(instance)));
                    if (dictValue == typeof(uint)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_UInt(pkg, (Dictionary<string, uint>)field.GetValue(instance)));
                    if (dictValue == typeof(long)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Long(pkg, (Dictionary<string, long>)field.GetValue(instance)));
                    if (dictValue == typeof(float)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Float(pkg, (Dictionary<string, float>)field.GetValue(instance)));
                    if (dictValue == typeof(double)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Double(pkg, (Dictionary<string, double>)field.GetValue(instance)));
                    if (dictValue == typeof(bool)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Bool(pkg, (Dictionary<string, bool>)field.GetValue(instance)));
                    if (dictValue == typeof(string)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_String(pkg, (Dictionary<string, string>)field.GetValue(instance)));
                    if (dictValue == typeof(Quaternion)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Quaternion(pkg, (Dictionary<string, Quaternion>)field.GetValue(instance)));
                    if (dictValue == typeof(Vector2i)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Vector2i(pkg, (Dictionary<string, Vector2i>)field.GetValue(instance)));
                    if (dictValue == typeof(Vector3)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Vector(pkg, (Dictionary<string, Vector3>)field.GetValue(instance)));
                    if (dictValue == typeof(Color)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Color(pkg, (Dictionary<string, Color>)field.GetValue(instance)));
                    if (dictValue == typeof(Color32)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_Color32(pkg, (Dictionary<string, Color32>)field.GetValue(instance)));
                    if (dictValue == typeof(ZPackage)) return ((object instance, ZPackage pkg) => SerializeDictionary_String_ZPackage(pkg, (Dictionary<string, ZPackage>)field.GetValue(instance)));
                }
            }

            if (t == typeof(int)) return ((object instance, ZPackage pkg) => SerializeInt(pkg, field.GetValue(instance)));
            if (t == typeof(uint)) return ((object instance, ZPackage pkg) => SerializeUInt(pkg, field.GetValue(instance)));
            if (t == typeof(long)) return ((object instance, ZPackage pkg) => SerializeLong(pkg, field.GetValue(instance)));
            if (t == typeof(float)) return ((object instance, ZPackage pkg) => SerializeFloat(pkg, field.GetValue(instance)));
            if (t == typeof(double)) return ((object instance, ZPackage pkg) => SerializeDouble(pkg, field.GetValue(instance)));
            if (t == typeof(bool)) return ((object instance, ZPackage pkg) => SerializeBool(pkg, field.GetValue(instance)));
            if (t == typeof(string)) return ((object instance, ZPackage pkg) => SerializeString(pkg, field.GetValue(instance)));
            if (t == typeof(Quaternion)) return ((object instance, ZPackage pkg) => SerializeQuaternion(pkg, field.GetValue(instance)));
            if (t == typeof(Vector2i)) return ((object instance, ZPackage pkg) => SerializeVector2i(pkg, field.GetValue(instance)));
            if (t == typeof(Vector3)) return ((object instance, ZPackage pkg) => SerializeVector(pkg, field.GetValue(instance)));
            if (t == typeof(Color)) return ((object instance, ZPackage pkg) => SerializeColor(pkg, field.GetValue(instance)));
            if (t == typeof(Color32)) return ((object instance, ZPackage pkg) => SerializeColor32(pkg, field.GetValue(instance)));
            if (t == typeof(ZPackage)) return ((object instance, ZPackage pkg) => SerializeZPackage(pkg, field.GetValue(instance)));

            return null;
        }
        #endregion

        #region DeserializeDelegates
        private static Action<object, ZPackage> GetDeserializeDelegate(FieldInfo field)
        {
            Type t = field.FieldType;
            if (t.GetInterface(nameof(ISerializableParameter)) != null) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeISP(pkg, t)));
            if (t.IsEnum) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeInt(pkg)));

            bool isList = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>);
            if (isList)
            {
                Type listInner = t.GetGenericArguments()[0];
                if (listInner.GetInterface(nameof(ISerializableParameter)) != null) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_ISP(pkg, listInner)));

                if (listInner == typeof(int)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Int(pkg)));
                if (listInner == typeof(uint)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_UInt(pkg)));
                if (listInner == typeof(long)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Long(pkg)));
                if (listInner == typeof(float)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Float(pkg)));
                if (listInner == typeof(double)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Double(pkg)));
                if (listInner == typeof(bool)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Bool(pkg)));
                if (listInner == typeof(string)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_String(pkg)));
                if (listInner == typeof(Quaternion)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Quaternion(pkg)));
                if (listInner == typeof(Vector2i)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Vector2i(pkg)));
                if (listInner == typeof(Vector3)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Vector(pkg)));
                if (listInner == typeof(Color)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Color(pkg)));
                if (listInner == typeof(Color32)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_Color32(pkg)));
                if (listInner == typeof(ZPackage)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeList_ZPackage(pkg)));
            }

            bool isDictionary = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
            if (isDictionary)
            {
                Type dictKey = t.GetGenericArguments()[0];
                if (dictKey != typeof(int) && dictKey != typeof(string))
                {
                    ZLog.LogError($"Error: Dictionary key type {dictKey} is not supported");
                    return null;
                }

                Type dictValue = t.GetGenericArguments()[1];
                if (dictValue.GetInterface(nameof(ISerializableParameter)) != null)
                {
                    if (dictKey == typeof(int)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_ISP(pkg, dictValue)));
                    if (dictKey == typeof(string)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_ISP(pkg, dictValue)));
                }

                if (dictKey == typeof(int))
                {
                    if (dictValue == typeof(int)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Int(pkg)));
                    if (dictValue == typeof(uint)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_UInt(pkg)));
                    if (dictValue == typeof(long)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Long(pkg)));
                    if (dictValue == typeof(float)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Float(pkg)));
                    if (dictValue == typeof(double)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Double(pkg)));
                    if (dictValue == typeof(bool)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Bool(pkg)));
                    if (dictValue == typeof(string)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_String(pkg)));
                    if (dictValue == typeof(Quaternion)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Quaternion(pkg)));
                    if (dictValue == typeof(Vector2i)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Vector2i(pkg)));
                    if (dictValue == typeof(Vector3)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Vector(pkg)));
                    if (dictValue == typeof(Color)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Color(pkg)));
                    if (dictValue == typeof(Color32)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_Color32(pkg)));
                    if (dictValue == typeof(ZPackage)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_Int_ZPackage(pkg)));
                }

                if (dictKey == typeof(string))
                {
                    if (dictValue == typeof(int)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Int(pkg)));
                    if (dictValue == typeof(uint)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_UInt(pkg)));
                    if (dictValue == typeof(long)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Long(pkg)));
                    if (dictValue == typeof(float)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Float(pkg)));
                    if (dictValue == typeof(double)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Double(pkg)));
                    if (dictValue == typeof(bool)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Bool(pkg)));
                    if (dictValue == typeof(string)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_String(pkg)));
                    if (dictValue == typeof(Quaternion)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Quaternion(pkg)));
                    if (dictValue == typeof(Vector2i)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Vector2i(pkg)));
                    if (dictValue == typeof(Vector3)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Vector(pkg)));
                    if (dictValue == typeof(Color)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Color(pkg)));
                    if (dictValue == typeof(Color32)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_Color32(pkg)));
                    if (dictValue == typeof(ZPackage)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDictionary_String_ZPackage(pkg)));
                }
            }

            if (t == typeof(int)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeInt(pkg)));
            if (t == typeof(uint)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeUInt(pkg)));
            if (t == typeof(long)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeLong(pkg)));
            if (t == typeof(float)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeFloat(pkg)));
            if (t == typeof(double)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeDouble(pkg)));
            if (t == typeof(bool)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeBool(pkg)));
            if (t == typeof(string)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeString(pkg)));
            if (t == typeof(Quaternion)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeQuaternion(pkg)));
            if (t == typeof(Vector2i)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeVector2i(pkg)));
            if (t == typeof(Vector3)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeVector(pkg)));
            if (t == typeof(Color)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeColor(pkg)));
            if (t == typeof(Color32)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeColor32(pkg)));
            if (t == typeof(ZPackage)) return ((object instance, ZPackage pkg) => field.SetValue(instance, DeserializeZPackage(pkg)));

            return null;
        }
        #endregion

        #region SerializeMethods
        private static void SerializeInt(ZPackage pkg, object value) => pkg.Write((int)value);
        private static void SerializeUInt(ZPackage pkg, object value) => pkg.Write((uint)value);
        private static void SerializeLong(ZPackage pkg, object value) => pkg.Write((long)value);
        private static void SerializeFloat(ZPackage pkg, object value) => pkg.Write((float)value);
        private static void SerializeDouble(ZPackage pkg, object value) => pkg.Write((double)value);
        private static void SerializeBool(ZPackage pkg, object value) => pkg.Write((bool)value);
        private static void SerializeString(ZPackage pkg, object value) => pkg.Write((string)value ?? "");
        private static void SerializeQuaternion(ZPackage pkg, object value) => pkg.Write((Quaternion)value);
        private static void SerializeVector2i(ZPackage pkg, object value) => pkg.Write((Vector2i)value);
        private static void SerializeVector(ZPackage pkg, object value) => pkg.Write((Vector3)value);
        private static void SerializeColor(ZPackage pkg, object value) => pkg.Write(global::Utils.ColorToVec3((Color)value));
        private static void SerializeColor32(ZPackage pkg, object value) => pkg.Write(global::Utils.ColorToVec3((Color32)value));
        private static void SerializeZPackage(ZPackage pkg, object value) => pkg.Write((ZPackage)value);

        private static void SerializeISP(ZPackage pkg, object value)
        {
            pkg.Write(value != null);
            ((ISerializableParameter)value)?.Serialize(ref pkg);
        }

        private static void SerializeList_Int(ZPackage pkg, List<int> value)
        {
            pkg.Write(value.Count);
            foreach (int i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_UInt(ZPackage pkg, List<uint> value)
        {
            pkg.Write(value.Count);
            foreach (uint i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_Long(ZPackage pkg, List<long> value)
        {
            pkg.Write(value.Count);
            foreach (long i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_Float(ZPackage pkg, List<float> value)
        {
            pkg.Write(value.Count);
            foreach (float i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_Double(ZPackage pkg, List<double> value)
        {
            pkg.Write(value.Count);
            foreach (double i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_Bool(ZPackage pkg, List<bool> value)
        {
            pkg.Write(value.Count);
            foreach (bool i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_String(ZPackage pkg, List<string> value)
        {
            pkg.Write(value.Count);
            foreach (string i in value)
            {
                pkg.Write(i ?? "");
            }
        }

        private static void SerializeList_Quaternion(ZPackage pkg, List<Quaternion> value)
        {
            pkg.Write(value.Count);
            foreach (Quaternion i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_Vector2i(ZPackage pkg, List<Vector2i> value)
        {
            pkg.Write(value.Count);
            foreach (Vector2i i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_Vector(ZPackage pkg, List<Vector3> value)
        {
            pkg.Write(value.Count);
            foreach (Vector3 i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_Color(ZPackage pkg, List<Color> value)
        {
            pkg.Write(value.Count);
            foreach (Color i in value)
            {
                pkg.Write(global::Utils.ColorToVec3(i));
            }
        }

        private static void SerializeList_Color32(ZPackage pkg, List<Color32> value)
        {
            pkg.Write(value.Count);
            foreach (Color32 i in value)
            {
                pkg.Write(global::Utils.ColorToVec3(i));
            }
        }

        private static void SerializeList_ZPackage(ZPackage pkg, List<ZPackage> value)
        {
            pkg.Write(value.Count);
            foreach (ZPackage i in value)
            {
                pkg.Write(i);
            }
        }

        private static void SerializeList_ISP(ZPackage pkg, IList value)
        {
            pkg.Write(value.Count);
            foreach (object i in value)
            {
                pkg.Write(i != null);
                ((ISerializableParameter)i)?.Serialize(ref pkg);
            }
        }

        private static void SerializeDictionary_Int_Int(ZPackage pkg, Dictionary<int, int> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, int> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_UInt(ZPackage pkg, Dictionary<int, uint> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, uint> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_Long(ZPackage pkg, Dictionary<int, long> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, long> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_Float(ZPackage pkg, Dictionary<int, float> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, float> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_Double(ZPackage pkg, Dictionary<int, double> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, double> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_Bool(ZPackage pkg, Dictionary<int, bool> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, bool> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_String(ZPackage pkg, Dictionary<int, string> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, string> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value ?? "");
            }
        }

        private static void SerializeDictionary_Int_Quaternion(ZPackage pkg, Dictionary<int, Quaternion> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, Quaternion> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_Vector2i(ZPackage pkg, Dictionary<int, Vector2i> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, Vector2i> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_Vector(ZPackage pkg, Dictionary<int, Vector3> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, Vector3> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_Color(ZPackage pkg, Dictionary<int, Color> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, Color> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(global::Utils.ColorToVec3(i.Value));
            }
        }

        private static void SerializeDictionary_Int_Color32(ZPackage pkg, Dictionary<int, Color32> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, Color32> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(global::Utils.ColorToVec3(i.Value));
            }
        }

        private static void SerializeDictionary_Int_ZPackage(ZPackage pkg, Dictionary<int, ZPackage> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<int, ZPackage> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_Int_ISP(ZPackage pkg, IDictionary value)
        {
            pkg.Write(value.Count);
            foreach (DictionaryEntry i in value)
            {
                pkg.Write((int)i.Key);
                pkg.Write(i.Value != null);
                ((ISerializableParameter)i.Value)?.Serialize(ref pkg);
            }
        }

        private static void SerializeDictionary_String_Int(ZPackage pkg, Dictionary<string, int> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, int> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_UInt(ZPackage pkg, Dictionary<string, uint> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, uint> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_Long(ZPackage pkg, Dictionary<string, long> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, long> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_Float(ZPackage pkg, Dictionary<string, float> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, float> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_Double(ZPackage pkg, Dictionary<string, double> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, double> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_Bool(ZPackage pkg, Dictionary<string, bool> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, bool> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_String(ZPackage pkg, Dictionary<string, string> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, string> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value ?? "");
            }
        }

        private static void SerializeDictionary_String_Quaternion(ZPackage pkg, Dictionary<string, Quaternion> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, Quaternion> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_Vector2i(ZPackage pkg, Dictionary<string, Vector2i> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, Vector2i> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_Vector(ZPackage pkg, Dictionary<string, Vector3> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, Vector3> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_Color(ZPackage pkg, Dictionary<string, Color> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, Color> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(global::Utils.ColorToVec3(i.Value));
            }
        }

        private static void SerializeDictionary_String_Color32(ZPackage pkg, Dictionary<string, Color32> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, Color32> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(global::Utils.ColorToVec3(i.Value));
            }
        }

        private static void SerializeDictionary_String_ZPackage(ZPackage pkg, Dictionary<string, ZPackage> value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, ZPackage> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value);
            }
        }

        private static void SerializeDictionary_String_ISP(ZPackage pkg, IDictionary value)
        {
            pkg.Write(value.Count);
            foreach (KeyValuePair<string, object> i in value)
            {
                pkg.Write(i.Key);
                pkg.Write(i.Value != null);
                ((ISerializableParameter)i.Value)?.Serialize(ref pkg);
            }
        }
        #endregion

        #region DeserializeMethods
        private static int DeserializeInt(ZPackage pkg) => pkg.ReadInt();
        private static uint DeserializeUInt(ZPackage pkg) => pkg.ReadUInt();
        private static long DeserializeLong(ZPackage pkg) => pkg.ReadLong();
        private static float DeserializeFloat(ZPackage pkg) => pkg.ReadSingle();
        private static double DeserializeDouble(ZPackage pkg) => pkg.ReadDouble();
        private static bool DeserializeBool(ZPackage pkg) => pkg.ReadBool();
        private static string DeserializeString(ZPackage pkg) => pkg.ReadString();
        private static Quaternion DeserializeQuaternion(ZPackage pkg) => pkg.ReadQuaternion();
        private static Vector2i DeserializeVector2i(ZPackage pkg) => pkg.ReadVector2i();
        private static Vector3 DeserializeVector(ZPackage pkg) => pkg.ReadVector3();
        private static Color DeserializeColor(ZPackage pkg) => global::Utils.Vec3ToColor(pkg.ReadVector3());
        private static Color32 DeserializeColor32(ZPackage pkg) => global::Utils.Vec3ToColor(pkg.ReadVector3());
        private static ZPackage DeserializeZPackage(ZPackage pkg) => pkg.ReadPackage();

        private static object DeserializeISP(ZPackage pkg, Type t)
        {
            bool hasValue = pkg.ReadBool();
            if (!hasValue) return null;
            object isp = Activator.CreateInstance(t);
            ((ISerializableParameter)isp).Deserialize(ref pkg);
            return isp;
        }

        private static List<int> DeserializeList_Int(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<int> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadInt());
            }

            return list;
        }

        private static List<uint> DeserializeList_UInt(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<uint> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadUInt());
            }

            return list;
        }

        private static List<long> DeserializeList_Long(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<long> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadLong());
            }

            return list;
        }

        private static List<float> DeserializeList_Float(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<float> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadSingle());
            }

            return list;
        }

        private static List<double> DeserializeList_Double(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<double> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadDouble());
            }

            return list;
        }

        private static List<bool> DeserializeList_Bool(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<bool> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadBool());
            }

            return list;
        }

        private static List<string> DeserializeList_String(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<string> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadString());
            }

            return list;
        }

        private static List<Quaternion> DeserializeList_Quaternion(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<Quaternion> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadQuaternion());
            }

            return list;
        }

        private static List<Vector2i> DeserializeList_Vector2i(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<Vector2i> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadVector2i());
            }

            return list;
        }

        private static List<Vector3> DeserializeList_Vector(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<Vector3> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadVector3());
            }

            return list;
        }

        private static List<Color> DeserializeList_Color(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<Color> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(global::Utils.Vec3ToColor(pkg.ReadVector3()));
            }

            return list;
        }

        private static List<Color32> DeserializeList_Color32(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<Color32> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(global::Utils.Vec3ToColor(pkg.ReadVector3()));
            }

            return list;
        }

        private static List<ZPackage> DeserializeList_ZPackage(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            List<ZPackage> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(pkg.ReadPackage());
            }

            return list;
        }

        private static IList DeserializeList_ISP(ZPackage pkg, Type t)
        {
            int count = pkg.ReadInt();
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(t));
            for (int i = 0; i < count; i++)
            {
                bool hasValue = pkg.ReadBool();
                if (!hasValue)
                {
                    continue;
                }

                object isp = Activator.CreateInstance(t);
                ((ISerializableParameter)isp).Deserialize(ref pkg);
                list.Add(isp);
            }

            return list;
        }

        private static Dictionary<int, int> DeserializeDictionary_Int_Int(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, int> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadInt();
            }

            return dict;
        }

        private static Dictionary<int, uint> DeserializeDictionary_Int_UInt(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, uint> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadUInt();
            }

            return dict;
        }

        private static Dictionary<int, long> DeserializeDictionary_Int_Long(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, long> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadLong();
            }

            return dict;
        }

        private static Dictionary<int, float> DeserializeDictionary_Int_Float(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, float> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadSingle();
            }

            return dict;
        }

        private static Dictionary<int, double> DeserializeDictionary_Int_Double(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, double> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadDouble();
            }

            return dict;
        }

        private static Dictionary<int, bool> DeserializeDictionary_Int_Bool(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, bool> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadBool();
            }

            return dict;
        }

        private static Dictionary<int, string> DeserializeDictionary_Int_String(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, string> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadString();
            }

            return dict;
        }

        private static Dictionary<int, Quaternion> DeserializeDictionary_Int_Quaternion(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, Quaternion> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadQuaternion();
            }

            return dict;
        }

        private static Dictionary<int, Vector2i> DeserializeDictionary_Int_Vector2i(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, Vector2i> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadVector2i();
            }

            return dict;
        }

        private static Dictionary<int, Vector3> DeserializeDictionary_Int_Vector(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, Vector3> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadVector3();
            }

            return dict;
        }

        private static Dictionary<int, Color> DeserializeDictionary_Int_Color(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, Color> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = global::Utils.Vec3ToColor(pkg.ReadVector3());
            }

            return dict;
        }

        private static Dictionary<int, Color32> DeserializeDictionary_Int_Color32(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, Color32> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = global::Utils.Vec3ToColor(pkg.ReadVector3());
            }

            return dict;
        }

        private static Dictionary<int, ZPackage> DeserializeDictionary_Int_ZPackage(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<int, ZPackage> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadInt()] = pkg.ReadPackage();
            }

            return dict;
        }

        private static IDictionary DeserializeDictionary_Int_ISP(ZPackage pkg, Type t)
        {
            int count = pkg.ReadInt();
            IDictionary dict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(int), t));
            for (int i = 0; i < count; i++)
            {
                object isp = Activator.CreateInstance(t);
                int key = pkg.ReadInt();
                bool hasValue = pkg.ReadBool();
                if (!hasValue)
                {
                    continue;
                }

                ((ISerializableParameter)isp).Deserialize(ref pkg);
                dict[key] = isp;
            }

            return dict;
        }

        private static Dictionary<string, int> DeserializeDictionary_String_Int(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, int> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadInt();
            }

            return dict;
        }

        private static Dictionary<string, uint> DeserializeDictionary_String_UInt(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, uint> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadUInt();
            }

            return dict;
        }

        private static Dictionary<string, long> DeserializeDictionary_String_Long(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, long> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadLong();
            }

            return dict;
        }

        private static Dictionary<string, float> DeserializeDictionary_String_Float(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, float> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadSingle();
            }

            return dict;
        }

        private static Dictionary<string, double> DeserializeDictionary_String_Double(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, double> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadDouble();
            }

            return dict;
        }

        private static Dictionary<string, bool> DeserializeDictionary_String_Bool(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, bool> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadBool();
            }

            return dict;
        }

        private static Dictionary<string, string> DeserializeDictionary_String_String(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, string> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadString();
            }

            return dict;
        }

        private static Dictionary<string, Quaternion> DeserializeDictionary_String_Quaternion(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, Quaternion> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadQuaternion();
            }

            return dict;
        }

        private static Dictionary<string, Vector2i> DeserializeDictionary_String_Vector2i(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, Vector2i> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadVector2i();
            }

            return dict;
        }

        private static Dictionary<string, Vector3> DeserializeDictionary_String_Vector(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, Vector3> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadVector3();
            }

            return dict;
        }

        private static Dictionary<string, Color> DeserializeDictionary_String_Color(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, Color> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = global::Utils.Vec3ToColor(pkg.ReadVector3());
            }

            return dict;
        }

        private static Dictionary<string, Color32> DeserializeDictionary_String_Color32(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, Color32> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = global::Utils.Vec3ToColor(pkg.ReadVector3());
            }

            return dict;
        }

        private static Dictionary<string, ZPackage> DeserializeDictionary_String_ZPackage(ZPackage pkg)
        {
            int count = pkg.ReadInt();
            Dictionary<string, ZPackage> dict = new();
            for (int i = 0; i < count; i++)
            {
                dict[pkg.ReadString()] = pkg.ReadPackage();
            }

            return dict;
        }

        private static IDictionary DeserializeDictionary_String_ISP(ZPackage pkg, Type t)
        {
            int count = pkg.ReadInt();
            IDictionary dict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), t));
            for (int i = 0; i < count; i++)
            {
                object isp = Activator.CreateInstance(t);
                string key = pkg.ReadString();
                bool hasValue = pkg.ReadBool();
                if (!hasValue)
                {
                    continue;
                }

                ((ISerializableParameter)isp).Deserialize(ref pkg);
                dict[key] = isp;
            }

            return dict;
        }
        #endregion
    }
}