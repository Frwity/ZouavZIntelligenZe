using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Turret : BaseEntity
{
    static public int cost = 10;

    [SerializeField]
    int damage = 40;
    [SerializeField]
    float buildDuration = 20f;
    [SerializeField]
    float range = 15f;
    [SerializeField]
    float focusRefreshRate = 0.5f;
    [SerializeField]
    float attackSpeed = 1f;
    [SerializeField]
    GameObject bulletPrefab = null;
    [SerializeField]
    Transform bulletSlot;

    float currentDuration = 0f;

    float cooldown = 0f;
    bool isUnderConstruction = false;
    Unit currentFocus = null;
    Image BuildGaugeImage;
    GameObject toRotate;

    protected override void Awake()
    {
        base.Awake();
        BuildGaugeImage = transform.Find("Canvas/BuildProgressImage").GetComponent<Image>();
        if (BuildGaugeImage)
        {
            BuildGaugeImage.fillAmount = 0f;
            BuildGaugeImage.color = GameServices.GetTeamColor(GetTeam());
        }
    }

    // Start is called before the first frame update
    override protected void Start()
    {
        base.Start();
        currentDuration = buildDuration;
        toRotate = transform.Find("ToRotate").gameObject;
    }

    override public void Init(ETeam _team)
    {
        base.Init(_team);
        isUnderConstruction = true;
    }

    // Update is called once per frame
    override protected void Update()
    {
        if (isUnderConstruction)
        {
            currentDuration -= Time.deltaTime;
            if (currentDuration <= 0f)
            {
                isUnderConstruction = false;
                InvokeRepeating("UpdateFocus", 0f, focusRefreshRate);
                BuildGaugeImage.fillAmount = 0f;
            }
            else if (BuildGaugeImage)
                BuildGaugeImage.fillAmount = 1f - currentDuration / buildDuration;
        }
        else
            ComputeAttack();
    }

    void UpdateFocus()
    {
        if (currentFocus == null || Vector3.Distance(currentFocus.transform.position, transform.position) > range)
        {
            Collider[] unitsCollider = Physics.OverlapSphere(transform.position, range, 1 << LayerMask.NameToLayer("Unit"));
            foreach (Collider unitCollider in unitsCollider)
            {
                Unit unit = unitCollider.GetComponent<Unit>();
                if (unit.GetTeam() != Team)
                {
                    currentFocus = unit;
                    return;
                }
            }
        }
    }

    void ComputeAttack()
    {
        if (cooldown > 0f)
            cooldown -= Time.deltaTime;

        if (cooldown <= 0f && currentFocus)
        {
            toRotate.transform.LookAt(currentFocus.transform.position);
            
            cooldown = attackSpeed;

            if (bulletPrefab)
            {
                GameObject newBullet = Instantiate(bulletPrefab, bulletSlot);
                newBullet.transform.parent = null;
                newBullet.GetComponent<Bullet>().ShootToward(currentFocus.transform.position - transform.position, Team);
            }

            currentFocus.AddDamage(damage);
        }
    }
}
