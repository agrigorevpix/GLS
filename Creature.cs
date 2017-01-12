//#undef Ai_Human
//#define Ai_Human

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


[RequireComponent (typeof(NavMeshAgent))]

public class Creature : MonoBehaviour
{
    #region Базовое
    [Header("Basic, don't touch!")]
    public GameObject floorPlane; // Земля по которой работает цель движения    
    public GameObject MoveMark; // значёк/индикатор в место, которое мы двигаемся
    public GameObject RespectDis, BaseMoodDis, HungerDis, EnergyDis, ThirstDis, HygieneDis, ImmunityDis, HealthDis; // ссылки на дисплей отображения
    public GameObject[] InvSlot; // Ссылки на слоты инвентаря.    
    public GameObject OverLookLight; // Свет вокруг объект в ночи
    public GameObject WaterSource; // Источник из которого собираемся пить.

    public MyGod DearLord; // Ссылка на MyGod

    
    [Header("Stats")]
    public GameObject[] Inventory = new GameObject[4];  // Инвентарь
    
    public Vector3 movementTargetPosition; //цель движения
    public Vector3 attackPos;
    public Vector3 lookAtPos;  // Переменные для разворота

    public float SpeedWalk, SpeedRun; // Скорость передвижения
    public float Respect, BaseMood, Hunger, Energy, Thirst, Temperature, Immunity, Health; // Переменные/параметры жизни
    public float rotateSpeed = 20.0f; // Скорость разворота
    public float RemainingDistance;   // Осталось до цели передвижения
    public float SleepRestoreEnergy;  // Сколько восстанавливаем энергии во время сна
    public float StartMinusTime;      // Записываем сюда каждые несколько секунд время из которого мы уже отняли параметры жизни
    public float DrinkRestore; // Переменная для оптимизации, запоминаем, сколько мы восстанавливаем жажды в минуту
    public float StopDis = 2f; // Дистанция, ближе которой мы не подходим к объекту.
    public float DebugStopDis = 2f; // Поправочное значение для стоп дистанс, если нам вдруг не хватит этого значения
    public float TakeDis = 3f;

    public int DangerousLevel; // Уровень опасности существа    

    //Состояния states
    [Header("states (for debug)")]
    public bool Herbivorous = true; // Тип питания
    public bool Predator = false;    
    public bool Run = false; // Переменная отвечающая за миниторинг выключения скрипта при бездействии из вне при смене активной пешки, чтобы не залагало.   
    public bool isAlive = true; //Проверяем жива ли пешка
    public bool IsBusy = false; // Проверка занятости, если занят, то ничего другого не делает  
    public bool CanRun = true;
    public bool IsBigCreature = false; // Проверка, если существо большое, то дистанция достижения цели больше.    
    public bool IsNearWater = false; // Проверка, стоим ли у воды
    public bool Drinking = false; // проверка, если мы уже пьём, то не запускаем 100500 короунтинов
    public bool NightPhase = false; // Активируем ночной режим существа
    public bool PlaceIsBusy = false; // Чекаем занято ли место и можем ли мы к нему двигаться

    //Новые состояния по которым всё должно нормально работать
    //таблица задач
    public bool Walk = false; // находится ли в движении
    public bool IsAsleep = false; // Проверка на сон
    public bool SearchingFood = false;      //Пока не ясно нужна ли проверка для занятости поиском еды, как отдельная переменная
    public bool IsWorking = false; // Проверка, если занимается работой отнимаем силы    
    public bool IsFighting = false; // Сражаемся ли
    public bool IsIdling = false; // бездельник
    public bool isActive = false; // Проверка, выделена ли пешка
    public bool IsBringing = false; // несем ли что-то на склад
    public bool CriticalEnergy = false, CriticalThirst = false, CriticalHunger = false; // Переменные для проверки критических состояний    


    //private Sprite SlotNull;         //Ссылка на стандартный спрайт слота инвентаря

    [HideInInspector]
    public Transform MyTransform;   //Ссылка на собственный трансформ

    [HideInInspector]
    public NavMeshAgent agent; // Ссылка на агента поиска пути

    [HideInInspector]
    public RaycastHit hit;  // Считываем координату удара мышкой

    [HideInInspector]
    public Ray ray;       // Луч из камеры в точку удара

    [HideInInspector]
    public Animator Anim;   // Ссылка на аниматора

    [HideInInspector]
    public Light BackLight;

    [HideInInspector]
    public float StartSleepHour; // Переменная для хранения времени начала сна

    [HideInInspector]
    public int StartSleepDay; //Переменная для хранения дня начала сна   
   

    #endregion


    #region ИИ

    public List<GameObject> Interesting;
    public bool Ai_Human = true;
    // public List<GameObject> Food;

    #endregion


    // Use this for initialization
    void Start()
    {
        CreatureStart();
    }

    // Update is called once per frame
    void Update()
    {
        CreatureUpdate();
    }

    void FixedUpdate()
    {
        CreatureFixedUpdate();
    }

    public void CreatureStart()
    {
        MyTransform = transform;
        StopDis += 2f; // Для того, чтобы все персонажи доставали до объекта
        DebugStopDis = 1f; // ну чтоб наверняка уже доставали
        Anim = GetComponent<Animator>(); // Ссылка на аниматора
        movementTargetPosition = transform.position; // Присваиваем цель передвижения на самих себя
        SearchingFood = false;
        agent = GetComponent<NavMeshAgent>();
        BackLight = OverLookLight.GetComponent<Light>(); // Ссылка на объект света дя оптимизации
        DearLord = GameObject.Find("Scripts").GetComponent<MyGod>();

        //SlotNull = InvSlot[0].GetComponent<Image>().sprite;
        //Interesting.Add(GameObject.FindGameObjectsWithTag("Interactive"););

        Inventory = new GameObject[4];
        for (int i = 0; i <= Inventory.Length - 1; i++)
            Inventory[i] = null;

        //HungerDis = GameObject.Find("Hunger");

        //Объеты с тэгом Interactive помещаем в массив, затем в листинг, после чего сортируем по имени
        GameObject[] Inter_Massive = GameObject.FindGameObjectsWithTag("Interactive");
        foreach (GameObject GO in Inter_Massive)
        {
            Interesting.Add(GO);
        }

        //Сортируем по имени интересные вещи для существа
        Interesting.Sort(delegate(GameObject x, GameObject y)
        {
            if (x.name == null && y.name == null) return 0;
            else if (x.name == null) return -1;
            else if (y.name == null) return 1;
            else return x.name.CompareTo(y.name);
        });

        InvokeRepeating("LiveParameters", 1, 1);
    }

    public void CreatureUpdate()
    {
        // Эту байду от сюда убирать нельзя, запрещаем активироваться, если не уважает
        // Хотя тоже не верно эта система работает, надо придумать, как закрыть просто доступ к активации!!!!!!!!!!!!
        if (Respect <= 10)
            isActive = false;
    }

    public void CreatureFixedUpdate()
    {
        #region Контроллер


        if (isActive)
        {
            Vars.SelectedChar = gameObject;

            if (Input.GetMouseButton(1))//is the left mouse button being clicked?
            {
                StopAllCoroutines();
                ray = Camera.main.ScreenPointToRay(Input.mousePosition); //Получаем координаты через главную камеру -> Чертим вектор в точку кликнутой мышью - > и выводим его на сцену            

                if (floorPlane.GetComponent<Collider>().Raycast(ray, out hit, 500.0f)) //Проверяем попала ли наша точка на "Пол"												
                {
                    movementTargetPosition = hit.point;//Записываем координаты нашей кликнутой точки
                    //Instantiate(MoveMark, movementTargetPosition, MoveMark.transform.rotation);
                }
            }
        }
       

        Debug.DrawLine((movementTargetPosition + transform.up * 2), movementTargetPosition, Color.red);

        // проверка на занятость точки назначения       

        if (Physics.Raycast(ray, out hit, 100))
        {
            if (DearLord.IsThisAlive(hit.collider.gameObject) || hit.collider.gameObject.tag == "Water") // Если на нашем месте стоит живое существо или это водопой
            {
                PlaceIsBusy = true;

                if (!IsBigCreature)
                    StopDis = 3f ;
                else
                    StopDis = 4f;

                agent.stoppingDistance = StopDis; // говорим, что остановочная дистанция 3
            }

            else if (DearLord.IsThisAlive(hit.collider.gameObject) && hit.collider.gameObject.tag != "Water") // Если на нашем месте нет ничего важного
            {                
                PlaceIsBusy = false;

                if (!IsBigCreature)
                    StopDis = 2f;
                else
                    StopDis = 3f;

                agent.stoppingDistance = StopDis; // говорим, что нормальная остановочная дистанция 0
            }
        }

        if (!CriticalEnergy)
        {
            Move(movementTargetPosition);
        }     

        else
        {
            agent.Stop();
            movementTargetPosition = MyTransform.position;
        }
        

        #endregion
    }

    /// <summary>
    /// движение к объекту
    /// </summary>
    /// <param name="Target"></param>
    /// <returns></returns>
    public bool Move(Vector3 Target)
    {
        if (isAlive) // && !CriticalEnergy)
        {
            //Debug.Log("RemainingDistance: " + RemainingDistance);
            agent.Resume();
            agent.SetDestination(Target);
            RemainingDistance = agent.remainingDistance;


            //Бег
            if (RemainingDistance > 12f)
            {
                //agent.SetDestination(Target);
                //Debug.Log("Target: " + Target);
                //Debug.Log("Работаем");
                //IsBusy = true;  
                AnimStandartization();
                CanselBusy();
                agent.speed = SpeedRun;

                IsIdling = false;

                Walk = true;   
                Run = true;                              
            }

                //Шаг
            else if (RemainingDistance > StopDis && !PlaceIsBusy)
            {
                //Debug.Log("CriticalEnergy3: " + CriticalEnergy);
                // Debug.Log("Идём");
                //IsBusy = true;
                AnimStandartization();
                CanselBusy();
                agent.speed = SpeedWalk;
                
                IsIdling = false;
                Run = false;

                Walk = true;
            }

                //шаг, если место занято (кто то там стоит)
            else if (RemainingDistance > StopDis && PlaceIsBusy)
            {
                //IsBusy = true;
                AnimStandartization();
                CanselBusy();
                agent.speed = SpeedWalk;
                
                IsIdling = false;
                Run = false;
                Walk = true;
            }

                //Стоим
            else if (RemainingDistance <= StopDis && !PlaceIsBusy && IsIdling == false)
            {
                // Debug.Log("Стоим");
                AnimStandartization();                
                Anim.SetBool("Idling", true);

                IsBusy = false;
                Run = false;
                Walk = false;

                IsIdling = true;

                return true;
            }

            //стоим, если свободно
            //шаг, если место занято (кто то там стоит)
            else if (RemainingDistance <= StopDis && PlaceIsBusy && IsIdling == false)
            {
                AnimStandartization();               
                Anim.SetBool("Idling", true);

                IsBusy = false;
                Run = false;
                Walk = false;

                IsIdling = true;

                return true;
            }
        }

          
            return false;       
    }

    /// <summary>
    /// Стандартизация анимации
    /// </summary>
    public void AnimStandartization()
    {        
        Anim.SetBool("ChopLow", false);
        Anim.SetBool("Idling", false);
        Anim.SetBool("Death", false);
        Anim.SetBool("Grab", false);        
        Anim.SetBool("Run", false);
        
        IsAsleep = false;
        IsIdling = false;
        IsBusy = false;        
        Walk = false;
        Run = false;        
    }


    /// <summary>
    /// Отмена действия
    /// </summary>
    public void CanselBusy()
    {
        SearchingFood = false;
        IsFighting = false;
        IsBringing = false;
        IsWorking = false;
        IsAsleep = false;
        IsIdling = false;
        IsBusy = false;
        Walk = false;
        Run = false;  
    }
      

    /// <summary>
    /// Поднятие предмета
    /// </summary>
    public bool PickUpick(GameObject go)
    {
        bool Pick = false;

        if (Vector3.Distance(go.transform.position, transform.position) > TakeDis)
        {
            Component co = go.GetComponent<PickUp>();

            if (co != null)
            {
                for (int i = 0; i <= Inventory.Length - 1; i++)  //Цикл, ищем в массиве свободные места
                {
                    //Debug.Log("Началось: " + i);
                    if (Inventory[i] == null) //&& Inventory.Length >= i)
                    {
                        if (Vector3.Distance(go.transform.position, transform.position) < TakeDis && !IsBigCreature)
                        {
                            Inventory[i] = go; //когда находим записываем в него заранее заготовленный объект
                            go.GetComponent<PickUp>().PickIt();

                            if (isActive)
                                DisplayInv();

                            //IsBusy = true;
                            //StartCoroutine(Take(go, i));

                            Pick = true;

                            break;
                        }

                        if (Vector3.Distance(go.transform.position, transform.position) < TakeDis && IsBigCreature)
                        {
                            Inventory[i] = go; //когда находим записываем в него заранее заготовленный объект
                            go.GetComponent<PickUp>().PickIt();

                            if (isActive)
                                DisplayInv();

                            Pick = true;
                            break;
                        }

                        else
                        {
                            Debug.Log(gameObject.name + " Говорит: - 'Я не могу это сделать слишком далеко!'");
                            break;
                        }
                       
                    }

                    else if (Inventory.Length <= i)
                    {
                        Debug.Log(gameObject.name + " Говорит: - Инвентарь полон!'");

                        Pick = false;
                        break;
                    }
                }
            }
        }

        return Pick;
    }


    /// <summary>
    /// Просто кладём в инвентарь эллемент
    /// </summary>
    /// <param name="go"></param>
    public void PutInInventory(GameObject go)
    {;
        for (int i = 0; i <= Inventory.Length - 1; i++) // Ищем свободные места
        {
            if (Inventory[i] == null) // Есть место, то кладём
            {
                Inventory[i] = go; // Кладём в инвентарь
                DisplayInv();      // Обновляем дисплей инвентаря
                break;
            }

            else if (Inventory[i] != null && i == Inventory.Length - 1)
                Debug.Log("Всё пиздец, инвентарь забит");
        }
    }

    /// <summary>
    /// Метод поднимания предмета. i элемент инвентаря в который положим его
    /// </summary>
    /// <param name="go"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    public IEnumerator Take(GameObject go, int i)
    {
        yield return new WaitForSeconds(1f);
        Anim.SetBool("Grab", false);
        yield return new WaitForSeconds(1);
        Inventory[i] = go; //когда находим записываем в него заранее заготовленный объект
        IsBusy = false;
    }

    /// <summary>
    /// Поиск жрачки и нахождение ближайшей цели
    /// </summary>
    /// <returns></returns>
    public IEnumerator FindFood()
    {
        yield return new WaitForSeconds(5);

        GameObject[] Food_Massive;
        int NumObj;

        Food_Massive = GameObject.FindGameObjectsWithTag("Meat");
        NumObj = CheckMassSlot(Food_Massive);

        if (NumObj >= 1)
        {
            if (Predator)
            {
                GameObject Food = FindAndGo("Meat");

                if (Vector3.Distance(Food.transform.position, MyTransform.position) < TakeDis && !IsBigCreature)
                    PickUpick(Food);

                if (Vector3.Distance(Food.transform.position, MyTransform.position) < TakeDis && IsBigCreature)
                    PickUpick(Food);

                if (Vector3.Distance(Food.transform.position, MyTransform.position) > TakeDis)
                    StartCoroutine(FindFood());

                else
                {
                    //IsBusy = false;
                    //SearchingFood = false;
                }
            }
        }

        else if (NumObj <= 1 && Herbivorous)
        {
            Food_Massive = GameObject.FindGameObjectsWithTag("Herbal");
            NumObj = CheckMassSlot(Food_Massive);

            if (NumObj >= 1)
            {
                GameObject Food = FindAndGo("Herbal");

                if (Vector3.Distance(Food.transform.position, MyTransform.position) < TakeDis && !IsBigCreature)
                    PickUpick(Food);

                if (Vector3.Distance(Food.transform.position, MyTransform.position) < TakeDis && IsBigCreature)
                    PickUpick(Food);

                if (Vector3.Distance(Food.transform.position, MyTransform.position) > TakeDis)
                    StartCoroutine(FindFood());

                else
                {
                    //IsBusy = false;
                    //SearchingFood = false;
                }
            }

            else
            {
                FindFruitFood();
            }
        }
    }

    /// <summary>
    /// Поиск ближайшего источника питья
    /// </summary>
    /// <returns></returns>
    public IEnumerator FindWater()
    {
        SearchingFood = true;
        yield return new WaitForSeconds(0);

        if (IsNearWater && !Drinking)
        {
            Drinking = true;
            StartCoroutine(Drink(WaterSource));
        }

        else
        {
            List<GameObject> Water = new List<GameObject>();

            GameObject[] Food_Massive = GameObject.FindGameObjectsWithTag("Water");

            foreach (GameObject get in Food_Massive)
                Water.Add(get);


            Water.Sort(delegate(GameObject t1, GameObject t2)
            {
                return Vector3.Distance(t1.transform.position, MyTransform.position).CompareTo(Vector3.Distance(t2.transform.position, MyTransform.position));
            });

           

            if (Vector3.Distance(Water[0].transform.position, MyTransform.position) < 100f)
            {
                movementTargetPosition = Water[0].transform.position;                   //Элемент для еды (Ближайшая цель)
            }

            else
            {
                Debug.Log("Всё пиздец, я заблудился(ась) и хуй знает где вода");
                SearchingFood = false;
            }

            if (Vector3.Distance(Water[0].transform.position, MyTransform.position) > TakeDis + 2f && IsBigCreature)
            {
                StartCoroutine(FindWater());
            }

            else if (Vector3.Distance(Water[0].transform.position, MyTransform.position) > TakeDis && !IsBigCreature)
                StartCoroutine(FindWater());



            //  else
            // SearchingFood = false;
        }
    }


    /// <summary>
    /// Поиск кустов с едой
    /// </summary>
    public void FindFruitFood()
    {
        GameObject[] Fruit_Massive = GameObject.FindGameObjectsWithTag("FruitBush"); // Собираем все фрукты в массив

        int IsHerbalHave = CheckMassSlot(Fruit_Massive); // Проверяем нашлись ли кусы

        if (IsHerbalHave >= 1) // если нашлись, двигигаемся к ним
        {
            GameObject Fruit = FindAndGo("FruitBush"); // двигаемся к ближайшим кустам

            if (Vector3.Distance(Fruit.transform.position, MyTransform.position) < TakeDis)
            {
                StartCoroutine(EatFruits(Fruit));
            }
            else
            {
               // SearchingFood = false;
            }
        }

        else
        {
            SearchingFood = false;
            Debug.Log(gameObject.name + " - Говорит: Не нашлось еды для меня :(");
        }
    }

    //Внимание питание с куста у животных является гавнокодом!!!!!!!!!!!!!!!!! yield return new WaitForSeconds(2); время не игровое!!!
    /// <summary>
    /// Жрём фрукты прямо с куста
    /// </summary>
    /// <param name="Fruits"></param>
    /// <returns></returns>
    public IEnumerator EatFruits(GameObject Fruits)
    {       
        Anim.SetBool("Grab", true);

        yield return new WaitForSeconds(2);

        GameObject Collected = Fruits.GetComponent<Resources>().Collecting(1);    // получаем объект чтобы пожрать
        // ТО ЧТО НИЖЕ ОЧЕНЬ ВАЖНО!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Дальше по идее должно быть что-то типа того 
        //Hunger += Collected.GetComponent<Food>().ResHunger;

        if (Collected != null)
        {
            //Hunger += Collected.GetComponent<PickUp>().RestHunger;
            Collected.GetComponent<PickUp>().RestoreParam(gameObject); // Восполняем параметры в зависимости, что за объект съеден
        }

        Anim.SetBool("Grab", false);
        IsBusy = false;
        SearchingFood = false;
    }

    //ВНИМАНИЕ!!!!!!!!!! НИЖЕ УСТАРЕВШИЙ МЕТОД !!!!! НАДО ЕГО НАХРЕН ОПТИМИЗИРОВАТЬ, ТАМ ПОЛНЫЙ ЗВИЗДЕЦ, АХРИНИТЕЛЬНЫЙ ОБМЕН ПОЦЕЛУЯМИ ЧЕРЕЗ 
    // GetComponent С WaterWell
    /// <summary>
    /// Просто пьём ;) устаревший метод, можно удалять скоро
    /// </summary>
    /// <param name="go"></param>
    /// <returns></returns>
    public IEnumerator Drink(GameObject go)
    {
        yield return new WaitForSeconds(ClocksGame.MinuteFormula * 0.3f);
        //yield return new WaitForSeconds(ClocksGame.MinuteFormula * 0.3f); //WaitForSeconds((ClocksGame.MinuteFormula * 30) / 100);
        //go.GetComponent<WaterWell>().DrinkWater(gameObject);
        Thirst += DrinkRestore;
        Drinking = false;

        if (Thirst >= 21)
        {
            StopCoroutine(FindWater());
            SearchingFood = false;
        }
        //Thirst += Num;
    }

    //DisplayInv() СРАНАЯ САДОМИЯ ОПТИМИЗИРОВАТЬ НАХРЕН!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! GetComponent творит беспредельное нечто! оптимизировать позже
    /// <summary>
    /// Отображение инвентаря на дисплее
    /// </summary>
    public void DisplayInv()
    {
        //InvSlot[0].GetComponent<Image>().sprite = Inventory[0].GetComponent<PickUp>().Icon;

        if (isActive) // отображаем инвентарь на дисплее если персонаж выделен
        {
            for (int i = 0; i <= Inventory.Length - 1; i++)
            {
                if (Inventory[i] != null) // Если что-то лежит в ячейке, выводи её иконку на экран
                {
                    InvSlot[i].GetComponent<Image>().color = new Color(1, 1, 1);  // Ставим насыщеность на максимум
                    InvSlot[i].GetComponent<Image>().sprite = Inventory[i].GetComponent<PickUp>().Icon; // Ставим иконку
                }

                else // иначе просто всё стираем
                {
                    InvSlot[i].GetComponent<Image>().sprite = Vars.SlotNull;
                    InvSlot[i].GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
                }
            }
        }

        else if (!isActive)        // Если персонаж не активен стираем всё с дисплея
            for (int i = 0; i <= Inventory.Length - 1; i++)
            {
                InvSlot[i].GetComponent<Image>().sprite = Vars.SlotNull;
                InvSlot[i].GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

                RespectDis.GetComponent<Text>().text = ("Уважение: ");
                HungerDis.GetComponent<Text>().text = ("Голод: ");
                ThirstDis.GetComponent<Text>().text = ("Жажда: ");
                HealthDis.GetComponent<Text>().text = ("Здоровье: ");
            }
    }

    /// <summary>
    /// Ложимся спать и восстанавливаем силы, ложимся прямо на месте,( больше подходит для животных) включать через InvokeRepeating() на всякий мать его случаай!
    /// </summary>
    public void Sleep()
    {
        IsAsleep = true;
        movementTargetPosition = MyTransform.position;
        

        /*
        IsAsleep = true;

        if(Energy >= 99)
            IsAsleep = false;

        if (IsAsleep)
        {
            float DifSleep = 0;
            if (StartSleepDay == Vars.DayNum)
                DifSleep = Vars.DayTime - StartSleepHour;

            else
                DifSleep = 0.1f;

            if (DifSleep >= 0.01f)
            {
                if (DifSleep <= 1)
                {
                    if (DifSleep <= 0.9f)
                    {
                        if (DifSleep >= 0.01f)
                        {
                            DifSleep = DifSleep * 10;
                        }
                        DifSleep = DifSleep * 10;                    
                    }
                }
                Energy += 0.084f * DifSleep + SleepRestoreEnergy;

                StartSleepDay = Vars.DayNum;
                StartSleepHour = Vars.DayTime;
            }

            // Запоминаем, когда легли спать, чтобы потом сравнивать сколько проспали с прошлого отрезка
            
        }
         */
    }


    /// <summary>
    /// Просто мать его просыпаемся
    /// </summary>
    public void Awake()
    {
        IsAsleep = false;
        CancelInvoke("Sleep");
    }

    /// <summary>
    /// Это некоего рода свой апдейт-счётчик, который отвечает за убывание параметров жизни относительно игрового времени.
    /// </summary>
    public void LiveParameters()
    {
        float TimeMinutepassed = DifferenceClock(StartMinusTime, Vars.DayTime);

        #region Критические параметры

        if (Energy <= 0)
        {
            movementTargetPosition = MyTransform.position;
            CriticalEnergy = true;
            IsAsleep = true;
        }

        if (Thirst <= 0)
            CriticalThirst = true;

        if (Hunger <= 0)
            CriticalHunger = true;

        if (Energy >= 15)
            CriticalEnergy = false;

        if (Thirst >= 5)
            CriticalThirst = false;

        if (Hunger >= 5)
            CriticalHunger = false;


        if (Hunger >= 101)
            Hunger = 100;

        if (Thirst >= 101)
            Thirst = 100;

        if (Energy >= 101)
            Energy = 100;

        #endregion

        #region Параметры Жизни

        // Если не спим, то начинаем голодать и жаждить
        if (!IsAsleep)
        {
            if (Hunger >= 0)
                Hunger -= 0.042f * TimeMinutepassed;

            if (Thirst >= 0)
                Thirst -= 0.084f * TimeMinutepassed;

            Anim.SetBool("Death", false);


            if (BaseMood <= 49)
                Anim.SetBool("Angry", true);

            //Когда засыпаем голодные, отнимаются хп
            if (Health >= 0 && Hunger <= 0)
                Health -= 0.006f * TimeMinutepassed;

            if (Health >= 0 && Thirst <= 0)
                Health -= 0.013f * TimeMinutepassed;

            if (Health <= 0)
                isAlive = false;

            /*
            //Напиши скрипт кровати блеать!!!!!!!!!!!!!!!!!!!!!!!!! Эту строку можно удалить и перенести в Human, животные всё равно при
            //уважении не будут искать где поспать у костра!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (Energy <= 20 && !IsAsleep && !isActive && Respect >= 19)
            {
                Debug.Log(gameObject.name + " - Говорит: Я заебался(ась), найдите мне кровать!!!");
            }

            else if (Energy <= 20 && !IsAsleep && !isActive && Respect <= 19)
            {
                Debug.Log(gameObject.name + " - Говорит: Я заебался(ась), уёбываюсь спать!!!");

                // Запоминаем, когда легли спать
                StartSleepDay = Vars.DayNum;
                StartSleepHour = Vars.DayTime;

                IsAsleep = true;
                //InvokeRepeating("Sleep", 1, 1);
            }
            */

            if (Energy <= 49 && CanRun)
                CanRun = false;
        }

        if (IsAsleep)
        {
            Energy += 0.084f * TimeMinutepassed + SleepRestoreEnergy * TimeMinutepassed; // Восстанавливаем количество энергии сна в зависимости от того, сколько времени прошло с момента последнего апдейта
            Anim.SetBool("Death", true);
        }

        if (Energy >= 99)
            IsAsleep = false;

        if (Walk || Run || IsWorking)
        {
            Energy -= 0.06f * TimeMinutepassed;
        }



        StartMinusTime = Vars.DayTime;

        if (!isAlive)
        {
            CancelInvoke("LiveParameters");
            Anim.SetBool("Death", true);
        }

        #endregion       

        #region AI

        if (IsNearWater && Thirst <= 90)
            StartCoroutine(Drink(WaterSource));

        else if (Thirst >= 90 && IsNearWater)
            Debug.Log(gameObject.name + " - Говорит: Меня уже тощнит от воды!");

        // RemainingDistance = agent.remainingDistance; // Хер знает что это, но пусть будет на всякий случай

        if (Thirst <= 20 && !isActive && !SearchingFood && !CriticalHunger && !IsAsleep || CriticalThirst && !IsAsleep)
        {
            SearchingFood = true;
            StartCoroutine(FindWater());
        }

        if (Hunger <= 20 && !isActive && !SearchingFood && !CriticalThirst && !IsAsleep || CriticalHunger && !CriticalThirst && !IsAsleep)
        {

            if (Hunger <= 20 && !isActive && !SearchingFood && !IsAsleep || CriticalHunger && !CriticalThirst && !IsAsleep)
            {
                // Алгоритм поиска жратвы животного
                if (!Ai_Human)
                {
                    SearchingFood = true;
                    StartCoroutine(FindFood());
                }

                //а люди умные звиздец, нихрена не хотят искать, только в телеге ищут
                if (Ai_Human)
                {
                    if (CheckFoodInChests("CookedMeat") == true)
                    CheckFoodInChests("CookedMeat");

                    else if (CheckFoodInChests("CookedHerbal") == true)
                    CheckFoodInChests("CookedHerbal");

                    else if (CheckFoodInChests("Meat") == true)
                        CheckFoodInChests("Meat");

                    else if (CheckFoodInChests("Herbal") == true)
                        CheckFoodInChests("Herbal");                    
                }
            }
        }  

        #endregion

        #region Ночная жизнь

        if (!Vars.IsDay)
        {
            NightPhase = true;
            if (Respect >= 19)
            {
                OverLookLight.SetActive(true);
            }
        }

        if (Vars.IsDay)
        {
            NightPhase = false;
            OverLookLight.SetActive(false);
        }

        //Возможно есть способ не обращаться постоянно к выключенному свету это кладёт производительность, покурить на досуге
        if (Respect <= 19)
        {
            OverLookLight.SetActive(false);
        }

        #endregion

        //БЛЯЯЯЯ!!!!!!!!!!!!!!!!!!! Сделай ты блин ссылки на объект, хрен ли ты над процессом издеваешься???!?!?!?!?!??!!?
        #region Display

        //БЛЯЯЯЯ!!!!!!!!!!!!!!!!!!! Сделай ты блин ссылки на объект, хрен ли ты над процессом издеваешься???!?!?!?!?!??!!?
        if (isActive && Respect >= 30)
        {
            RespectDis.GetComponent<Text>().text = ("Уважение: " + Respect);
            HungerDis.GetComponent<Text>().text = ("Голод: " + Hunger);
            ThirstDis.GetComponent<Text>().text = ("Жажда: " + Thirst);
            EnergyDis.GetComponent<Text>().text = ("Энергия: " + Energy);
            HealthDis.GetComponent<Text>().text = ("Здоровье: " + Health);
        }

        #endregion
    }
 

    /// <summary>
    /// Проверка массива на наличие свободных в нём мест, возвращает количество свободных слотов в инвентаре
    /// </summary>
    /// <param name="Mass"></param>
    /// <returns></returns>
    public int CheckMassEmptySlot(GameObject[] Mass)
    {
        int NumEmpty = 0;
        for (int i = 0; i <= Mass.Length - 1; i++)
        {
            if (Mass[i] == null) // Если что-то лежит в ячейке, выводи её иконку на экран
            {
                NumEmpty++;
            }
        }

        // Debug.Log("Количество свободных ячеек в массиве " + NumEmpty);
        return NumEmpty;
    }

    /// <summary>
    /// Проверка содержит ли массив хоть какаие то элементы или является просто полым, возвращает количество существующих объектов.
    /// </summary>
    /// <param name="Mass"></param>
    /// <returns></returns>
    public int CheckMassSlot(GameObject[] Mass)
    {
        int Num = 0;
        for (int i = 0; i <= Mass.Length - 1; i++)
        {
            if (Mass[i] != null)
            {
                Num++;
            }
        }

        return Num;
    }

    /// <summary>
    /// Найти объекты по тэгу и двигаться к ближайшему
    /// </summary>
    /// <param name="NameSearchObject"></param>
    /// <returns></returns>
    public GameObject FindAndGo(string NameSearchObject)
    {

        List<GameObject> go = new List<GameObject>();

        GameObject[] Object_Massive = GameObject.FindGameObjectsWithTag(NameSearchObject);

        foreach (GameObject get in Object_Massive)
            go.Add(get);


        go.Sort(delegate(GameObject t1, GameObject t2)
        {
            return Vector3.Distance(t1.transform.position, MyTransform.position).CompareTo(Vector3.Distance(t2.transform.position, MyTransform.position));
        });

        if (Vector3.Distance(go[0].transform.position, MyTransform.position) < 100f)
        {
            movementTargetPosition = go[0].transform.position;                   //Элемент для передвижения (Ближайшая цель)
            return go[0];
        }

        else
        {
            Debug.Log(gameObject.name + " - Говорит: Всё пиздец, я заблудился(ась) и хуй знает где объект");
            return null;
        }
    }


    /// <summary>
    /// Движение к какому-то объекту
    /// </summary>
    /// <param name="MovePoint"></param>
    public void GoToObject(GameObject MovePoint)
    {
        movementTargetPosition = MovePoint.transform.position;        //Куда идём   
    }


    /// <summary>
    /// Калькулятор разницы времени, возвращает значение количества времени в промежутке от start до end
    /// </summary>
    /// <param name="StartTime"> Время от которого считаем сколько прошло времени </param>
    /// <param name="EndTime"> Финальное время до которого считаем </param>
    /// <returns></returns>
    public float DifferenceClock(float StartTime, float EndTime)
    {
        float DifferenceTime = EndTime - StartTime;

            if (DifferenceTime >= 0.01f)
            {
                if (DifferenceTime <= 1)
                {
                    if (DifferenceTime <= 0.9f)
                    {
                        if (DifferenceTime >= 0.01f)
                        {
                            DifferenceTime = DifferenceTime * 10;
                        }

                        DifferenceTime = DifferenceTime * 10;
                    }
                }
            }

            if (DifferenceTime >= 0)
                return DifferenceTime;

            else
                return 0;
    }
        
    public bool CheckIsNearTaget(Transform Target1, Transform Target2, float NeedDis)
    {
        if (Vector3.Distance(Target1.position, Target2.position) > NeedDis)
            return true;

        return false;
    }

    /// <summary>
    /// Ищем все сундуки и ищем в них входной параметр и двигаемся к ближайшему если находим в нём нужное и жрём это.
    /// Возвращаем true, если нашёл в каком то сундуке искомый объект.
    /// </summary>
    /// <param name="WhatFind"></param>
    /// <returns></returns>
    public bool CheckFoodInChests(string TextTag)
    {
        bool IsHavingFood = false;

        List<GameObject> go = new List<GameObject>();

        GameObject[] Object_Massive = GameObject.FindGameObjectsWithTag("Chest");

        foreach (GameObject get in Object_Massive)
            go.Add(get);


        go.Sort(delegate(GameObject t1, GameObject t2)
        {
            return Vector3.Distance(t1.transform.position, MyTransform.position).CompareTo(Vector3.Distance(t2.transform.position, MyTransform.position));
        });

        Chest CHslots;

        for (int i = 0; i <= Object_Massive.Length - 1; i++) // Обыскиваем с расстояния все сундуки отсортированные по дистанции
        {
            CHslots = go[i].GetComponent<Chest>(); // Ссылка на ближайший сундук
            bool get = CHslots.CheckContent(TextTag); // Есть ли что в сундуке?

            if (get == true) // Есди есть, то двигаемся к нему
            {
                IsHavingFood = true;
                if (Vector3.Distance(go[i].transform.position, MyTransform.position) < 100f)
                {
                    if (Vector3.Distance(go[i].transform.position, MyTransform.position) > TakeDis + 3f)
                    {
                        SearchingFood = true;
                        movementTargetPosition = go[i].transform.position;        //Элемент Ближайшая цель
                    }

                    if (Vector3.Distance(go[i].transform.position, MyTransform.position) <= TakeDis + 1f)
                    {
                        CHslots.TakeOut(TextTag).GetComponent<PickUp>().RestoreParam(gameObject); // Забираем из сундука жратву и хаваем

                        IsBusy = false;
                        SearchingFood = false;
                    }

                    break;
                }

                else
                {
                    IsHavingFood = false;
                    Debug.Log(gameObject.name + " - Говорит: Всё писец, я заблудился(ась) и хрен знает где объект");
                    SearchingFood = false;
                    break;
                }
            }

            else
            {
                IsHavingFood = false;
                Debug.Log(gameObject.name + " - Говорит: Всё писец, жрать нечего!");
            }
        }

        return IsHavingFood;
    }

    /// <summary>
    /// Ищем все сундуки и ищем в них входной параметр и двигаемся к ближайшему если находим в нём нужное и по дополнительной команде 
    /// забираем содержимое сундука, возвращает значение true, если нашёл в каком то сундуке искомый объект.
    /// </summary>
    /// <param name="WhatFind">Что ищем</param>
    /// <returns>Забираем это?</returns>
    public bool CheckChests(string TextTag, bool AreTake)
    {
        bool IsHavingGO = false;

        List<GameObject> go = new List<GameObject>();

        GameObject[] Object_Massive = GameObject.FindGameObjectsWithTag("Chest");

        foreach (GameObject get in Object_Massive)
            go.Add(get);


        go.Sort(delegate(GameObject t1, GameObject t2)
        {
            return Vector3.Distance(t1.transform.position, MyTransform.position).CompareTo(Vector3.Distance(t2.transform.position, MyTransform.position));
        });

        Chest CHslots;

        for (int i = 0; i <= Object_Massive.Length - 1; i++) // Обыскиваем с расстояния все сундуки отсортированные по дистанции
        {
            CHslots = go[i].GetComponent<Chest>(); // Ссылка на ближайший сундук
            bool get = CHslots.CheckContent(TextTag); // Есть ли что в сундуке?

            if (get == true) // Есди есть, то стреляем расстояние до него
            {
                IsHavingGO = true; // Нашли объект
                if (Vector3.Distance(go[i].transform.position, MyTransform.position) < 100f)
                {
                    if (Vector3.Distance(go[i].transform.position, MyTransform.position) > TakeDis) // Если мы далековато, то мы не рядом
                    {
                        movementTargetPosition = go[i].transform.position;        //Элемент Ближайшая цель                        
                    }

                    if (Vector3.Distance(go[i].transform.position, MyTransform.position) < TakeDis) // Если мы рядом, то мы ж блин рядом!
                    {
                        if (AreTake)
                        {
                            GameObject TakeIt = CHslots.TakeOut(TextTag); // забираем из сундука

                            PutInInventory(TakeIt); // Кладём себе в рюкзак
                        }
                    }

                    break;
                }

                else
                {
                    Debug.Log(gameObject.name + " - Говорит: Всё писец, я заблудился(ась) и хрен знает где объект");
                    IsHavingGO = false;
                    break;
                }
            }

            else
            {
                //Debug.Log(gameObject.name + " - Говорит: Всё писец, объетов в сундуках нет этого сраного " + TextTag);                
                IsHavingGO = false;
            }
        }

        return IsHavingGO;
    }
}



/* ПОИСК ЕДЫ В ИНВЕНТАРЕ!!!!!!!!! ПУСТЬ ОСТАНЕТСЯ ВДРУГ ПРИГОДИТСЯ!!!
            * 
           for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory еду
           {
               if (Inventory[i] != null && Inventory[i].tag == ("Meat") && Predator) // ищем еду для хищников
               {
                   Inventory[i].GetComponent<PickUp>().RestoreParam(gameObject); // Восполняем параметры в зависимости, что за объект съеден
                   Inventory[i] = null;
                   // Destroy(Inventory[i]);
                   break;
               }

               if (Hunger <= 20 && Inventory[i] != null && Inventory[i].tag == ("Herbal") && Herbivorous) //ищем еду для травоядных   
               {
                   Inventory[i].GetComponent<PickUp>().RestoreParam(gameObject); // Восполняем параметры в зависимости, что за объект съеден
                   // Destroy(Inventory[i]);
                   Inventory[i] = null;
                   break;
               }
           }
           */



/*
          //Бег
          if (RemainingDistance >12f)
          {                
              //agent.SetDestination(Target);
              //Debug.Log("Target: " + Target);
              //Debug.Log("Работаем");
              agent.speed = SpeedRun;
              IsBusy = true;
              Anim.SetBool("ChopLow", false);
              Anim.SetBool("Grab", false);
              Anim.SetBool("Idling", false);
              Anim.SetBool("Run", true);
              IsAsleep = false;
              Anim.SetBool("Death", false);
              Run = true;
              Walk = false;
          }

              //Шаг
          else if (RemainingDistance > 0.1f)
          {
              //Debug.Log("CriticalEnergy3: " + CriticalEnergy);
              // Debug.Log("Идём");
              agent.speed = SpeedWalk;
              IsBusy = true;
              Anim.SetBool("ChopLow", false);
              Anim.SetBool("Grab", false);
              Anim.SetBool("Run", false);
              Anim.SetBool("Idling", false);
              IsAsleep = false;
              Anim.SetBool("Death", false);
              Run = false;
              Walk = true;
          }

              //Стоим
          else if (RemainingDistance == 0)
          {
              // Debug.Log("Стоим");
              IsBusy = false;
              Anim.SetBool("Idling", true);
              Anim.SetBool("Run", false);
              Run = false;
              Walk = false;

              return true;
          }

      }

      else
          agent.Stop();

          */

/*
if (Vector3.Distance(Target, transform.position) > 5f)
{
    IsBusy = true;
    Anim.SetBool("ChopLow", false);
    Anim.SetBool("Grab", false);
    Anim.SetBool("Idling", false);
    Anim.SetBool("Run", true);
    Run = true;
    transform.position += transform.forward * SpeedRun * Time.deltaTime;
}

else if (Vector3.Distance(Target, transform.position) > 1.4f && !IsBigCreature)
{
    IsBusy = true;
    Anim.SetBool("ChopLow", false);
    Anim.SetBool("Grab", false);
    Anim.SetBool("Run", false);
    Anim.SetBool("Idling", false);
    Run = false;
    transform.position += transform.forward * SpeedWalk * Time.deltaTime;
}

else if (Vector3.Distance(Target, transform.position) > 3f && IsBigCreature)
{
    IsBusy = true;
    Anim.SetBool("ChopLow", false);
    Anim.SetBool("Grab", false);
    Anim.SetBool("Run", false);
    Anim.SetBool("Idling", false);
    Run = false;
    transform.position += transform.forward * SpeedWalk * Time.deltaTime;
}

else if (Vector3.Distance(Target, transform.position) <= 3f && IsBigCreature)
{
    IsBusy = false;
    Anim.SetBool("Idling", true);
    Anim.SetBool("Run", false);
    Run = false;

    return true;
}

else if (Vector3.Distance(Target, transform.position) <= 1.4f && !IsBigCreature)
{
    IsBusy = false;
    Anim.SetBool("Idling", true);
    Anim.SetBool("Run", false);
    Run = false;

    return true;
}
}
 */





/*
//Бег
if (RemainingDistance >12f)
{                
//agent.SetDestination(Target);
//Debug.Log("Target: " + Target);
//Debug.Log("Работаем");
AnimStandartization();
agent.speed = SpeedRun;
IsBusy = true;                
Run = true;
Walk = false;
}

//Шаг
else if (RemainingDistance > 0.1f && !PlaceIsBusy)
{
//Debug.Log("CriticalEnergy3: " + CriticalEnergy);
// Debug.Log("Идём");
AnimStandartization();
agent.speed = SpeedWalk;
IsBusy = true;
                
Run = false;
Walk = true;
}
                
//шаг, если место занято (кто то там стоит)
else if (RemainingDistance > 3.1f && PlaceIsBusy)
{
AnimStandartization();
agent.speed = SpeedWalk;
IsBusy = true;
                
Run = false;
Walk = true;
}

//Стоим
else if (RemainingDistance == 0 && !PlaceIsBusy)
{
// Debug.Log("Стоим");
AnimStandartization();
IsBusy = false;
Anim.SetBool("Idling", true);                
Run = false;
Walk = false;

return true;
}

//стоим, если свободно
//шаг, если место занято (кто то там стоит)
else if (RemainingDistance > 3f && PlaceIsBusy)
{
AnimStandartization();
IsBusy = false;
Anim.SetBool("Idling", true);
                
Run = false;
Walk = false;

return true;
}
*/
