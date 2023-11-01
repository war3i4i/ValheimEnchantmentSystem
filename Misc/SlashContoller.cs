using System.Collections.Generic;
using UnityEngine;

namespace kg.ValheimEnchantmentSystem.Misc;

public class SlashContoller : MonoBehaviour
{
    private ZNetView _znv;

    private readonly ParticleSystem[] _slashParticles = new ParticleSystem[4];
    private Transform Rotation;
    private readonly HashSet<IDestructible> list = new();

    private Color _color
    {
        get => global::Utils.Vec3ToColor(_znv.GetZDO().GetVec3("slashColor", Vector3.zero));
        set => _znv.GetZDO().Set("slashColor", global::Utils.ColorToVec3(value));
    }

    private int _damage
    {
        get => _znv.GetZDO().GetInt("slashDamage", 0);
        set => _znv.GetZDO().Set("slashDamage", value);
    }
    
    private int _randomRotation
    {
        get => _znv.GetZDO().GetInt("slashRandomRotation", 0);
        set => _znv.GetZDO().Set("slashRandomRotation", value);
    }

    private Vector3 _dir = Vector3.zero;

    private void Awake()
    {
        _znv = GetComponent<ZNetView>();
        Rotation = transform.Find("effect");
        _slashParticles[0] = transform.Find("effect/1").GetComponent<ParticleSystem>();
        _slashParticles[1] = transform.Find("effect/2").GetComponent<ParticleSystem>();
        _slashParticles[2] = transform.Find("effect/3").GetComponent<ParticleSystem>();
        _slashParticles[3] = transform.Find("effect/4").GetComponent<ParticleSystem>();
        if (_znv.IsOwner()) return;
        
        var euler = Rotation.localRotation.eulerAngles;
        Rotation.localRotation = Quaternion.Euler(_randomRotation, euler.y, euler.z);

        foreach (var slashParticle in _slashParticles)
        {
            var main = slashParticle.main;
            main.startColor = _color;
        }
    }
    

    public void Init(Color color, int damage, Vector3 dir)
    {
        _dir = dir;
        _color = color;
        foreach (var slashParticle in _slashParticles)
        {
            var main = slashParticle.main;
            main.startColor = color;
        }

        _damage = damage;
        int rRot = Random.Range(0, 360);
        _randomRotation = rRot;
        var euler = Rotation.localRotation.eulerAngles;
        Rotation.localRotation = Quaternion.Euler(rRot, euler.y, euler.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_znv.IsOwner()) return;
        Character c = other.GetComponentInParent<Character>();
        if (!c || !c.IsEnemy()) return;
        Vector3 point = other.ClosestPointOnBounds(transform.position);
        HitData hit = new()
        {
            m_attacker = Player.m_localPlayer.GetZDOID(),
            m_point = point,
            m_skill = Skills.SkillType.ElementalMagic
        };
        hit.m_damage.m_slash = _damage;
        hit.m_ranged = true;
        c.Damage(hit);
        Instantiate(Enchantment_Core.SlashPrefab_Explosion, point, Quaternion.identity);
        list.Add(c);
    }

    private float count;
    private void Update()
    {
        if (!_znv.IsOwner()) return;
        if (!Player.m_localPlayer)
        {
            _znv.ClaimOwnership();
            ZNetScene.instance.Destroy(this.gameObject);
            return;
        }
        const float speed = 15f;
        count += Time.deltaTime;
        transform.position += _dir * speed * Time.deltaTime;
        if (transform.position.y <= 30)
        {
            _znv.ClaimOwnership();
            ZNetScene.instance.Destroy(this.gameObject);
            return;
        }

        if (count <= 2f) return;

        _znv.ClaimOwnership();
        ZNetScene.instance.Destroy(this.gameObject);
    }

    public static void CreateNewSlash(GameObject prefab, Vector3 position, Vector3 rotation, Color color, int damage)
    {
        Instantiate(prefab, position, Quaternion.LookRotation(rotation)).GetComponent<SlashContoller>().Init(color, damage, rotation);
    }
}