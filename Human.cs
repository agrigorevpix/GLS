using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class Human : Creature 
{
    [Header("Basic, don't touch!")]
    public GameObject ProfessionDropDown; // Ссылка на дропдаун ЮИ
    public Vars Varis; // Ссылка на вариэйблы, для сохранения ресурсов

    [Header("Human Stats")]
    public float Age = 0; // Сколько нам годиков
    public bool IsFemale = false; // Пол

    // Тут главное не забыть прокачку каждого скила по достижению опыта определённого    
    public float WoodExp = 0, FarmExp = 0, MasonExp = 0, WarExp = 0, BlackExp = 0, HerbalistExp = 0; // Опыт

    public int WoodSkill = 0, FarmSkill = 0, MasonSkill = 0, WarSkill = 0, BlackSkill = 0, HerbalistSkill = 0; // Скиллы
    public int InstrumentPoint = 0;  // Множитель инструмента в руке, не добывает, если равен нулю

    [Header("Active profession (for debug)")]
    public bool IsAdult = false;
    //
    public bool WoodWorker, Farmer, Mason, Warrior, BlackSmith, Herbalist;               // Пэрент Профессия
    //          Древоруб  Древострой   Мебель    Сажает 
    public bool Woodcutter, Builder, Carpenter, Forester;                              // Дочерняя профа WoodWorker
    //          Сборягод       повар Выращивает  Скотовод
    public bool FoodCollector, Cook, Cultivator, Grazier;                            // Дочерняя профа Farmer
    //         Рудокоп Кирпичедел  Камнестрой    Скульптор
    public bool Miner, Bricklayer, StoneBuilder, Sculptor;                         // Дочерняя профа Mason
    //           Страж     Охотник Главнокомандующий
    public bool Sentinel , Hunter , Marshal;                                     // Дочерняя профа Warrior
    //     Базвые инструменты Прокач инстр, швея
    public bool CraftMaster, ToolMaster, Tailor;                               // Дочерняя профа BlackSkill
    //           Сборщик травы Траволечебодел, Хилер(Выводит из запоя и комы)  
    public bool HerbalColletor, Pharmacist, Healer;                          // Дочерняя профа HerbalistSkill    
    //Главное теперь новое говно не забывать сбрасывать в BecomeNobody()

    public bool Farming = false, TakingSupplies = false; // Собирает ли, и тащит ли

    [Header("Family Params")]
    public GameObject MyHome;
    public GameObject[] MyParents;
    public GameObject[] MyChildren;
    public GameObject MySpouse, PotentialChildren;
      

    bool IsNightDebuf = false, NightDebufed = false; // Активация Дебафа на ночь и проверка, если один раз дебафнули или разбафали, больше не бафаем
    
    float HumanMinusTime; // Вспомогательная переменная для засекания времени сколько работает горожанин
    

    // Проверочки, чтобы их не разрывало между разными задачами
    
    [Header("Basic, don't touch!")]
    public GameObject CookedMeat;         // Ссылка на изготовленное мясо
    public GameObject CookedVegetables;  // Ссылка на изготовленные травы
   
    public Dropdown ChgDropDown; // Ссылка для изменения дропдауна при изменении профессий

    void Start()
    {
        CreatureStart();
        InvokeRepeating("HumanUpdate", 1, 1);
        Ai_Human = true;
        ChgDropDown = ProfessionDropDown.GetComponent<Dropdown>();

        //Не забыть ещё сделать метод взросления, когда повзрослеет, чтобы получить леваки!!!!!!!!!!!!!!!
        // Если ты взросел то тебе пиздосел!!!
        if (Age >= 10)
        {
            IsAdult = true;
            WoodSkill = 1; 
            FarmSkill = 1; 
            MasonSkill = 1; 
            WarSkill = 1;
            BlackSkill = 1; 
            HerbalistSkill = 1;
        }
            // Если слишком юн, то ты мудак!!!
        else
        {
            IsAdult = false;
            WoodSkill = 0;
            FarmSkill = 1; 
            MasonSkill = 0;
            WarSkill = 0;
            BlackSkill = 0;
            HerbalistSkill = 1;
        }
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

    // Update вызывается раз в секунду
    public void HumanUpdate()
    {

        float TimeMinutepassed;

        if (WoodWorker || Farmer || Mason || Warrior || BlackSmith || Herbalist)
        {
            TimeMinutepassed = DifferenceClock(StartMinusTime, Vars.DayTime);
        }

        else
            TimeMinutepassed = 0;

        //Работаем по профессии, если не спим
        if (!IsAsleep)
        {

            #region Дневаня фаза

            if (!NightPhase)
            {
                if (Energy <= 20)
                {
                    FindPlaceToSleep(); // Ищем подходящее место для сна
                }

                if (!IsNightDebuf && NightDebufed)
                {
                    NightDebufed = false;
                    NightDebuf(false);
                }

                //Если день и мало энергии,мы уважаем игрока и не активны (выделены) и не спим, то ищем место поспать
                if (Energy <= 20 && !IsAsleep && !isActive && Respect >= 19)
                {
                    Debug.Log(gameObject.name + " - Говорит: Я заебался(ась), найдите мне кровать!!!");
                    FindPlaceToSleep();
                }

                    //Если мы не спим, но хотим спать, не уважаем игрока, уёбываемся спать, даже если выделены
                else if (Energy <= 20 && !IsAsleep && Respect <= 19)
                {
                    Debug.Log(gameObject.name + " - Говорит: Я заебался(ась), уёбываюсь спать!!!");

                    // Запоминаем, когда легли спать
                    StartSleepDay = Vars.DayNum;
                    StartSleepHour = Vars.DayTime;

                    IsAsleep = true;
                    //InvokeRepeating("Sleep", 1, 1);
                }
            }

            #endregion

            #region Ночная фаза

            if (NightPhase) // Условия ночной фазы
            {
                IsNightDebuf = true; // Включаем ночной дебаф

                //Усталось и сон (Если усталость достигает низкой отметки)
                if (Energy <= 20)
                {
                    FindPlaceToSleep(); // Ищем подходящее место для сна
                }

                // Если ночь и ещё не дебафнулись, то вешаем дебаф
                if (IsNightDebuf && !NightDebufed)
                {
                    NightDebuf(true); // Включаем дебаф
                    NightDebufed = true; // Для профверки, говорим, что уже дебафнуты, чтобы постоянно не запускать условие
                }
            }

            else // Переход в дневную фазу
            {
                IsNightDebuf = false; // Выключаем ночной дебаф

                if (!IsNightDebuf && NightDebufed)
                {
                    NightDebufed = false;
                    NightDebuf(false);
                }
            }

            #endregion

            #region Работа / Профессии

            if (!SearchingFood && !IsAsleep && !IsFighting && !CriticalEnergy && !CriticalHunger && !CriticalThirst) // Проверка можно ли включать работу
            {
                if (WoodWorker && !SearchingFood && !IsWorking) // WoodWorker
                {
                    WoodExp += 0.1f;// * TimeMinutepassed; // получаем опыт за работу дровосеком

                    if (Woodcutter) // Дровосек
                        FindWood();

                    // Если под профессия строитель, то строим
                    if (Builder)
                        FindBuild();

                    if (Carpenter)
                        Debug.Log("нет такой профессии");

                    if (Forester)
                        Debug.Log("нет такой профессии");

                    //Прокачка 
                    if (WoodExp >= 12 && IsAdult && WoodSkill <= 1)
                    {
                        WoodSkill++;
                        ChgDropDown.options[1].text = "WoodWorker " + WoodSkill; // Если вдруг прокачиваемся меняем значение уровня профессии
                    }
                }

                if (Farmer && !SearchingFood && !IsWorking) // Farmer
                {
                    FarmExp += 0.1f;// * TimeMinutepassed; // Получаем опыт за работу

                    if (FoodCollector)
                        FindFruit();

                    if (Cook)
                    {
                        if (FarmExp >= 2)
                        {
                            if(CheckCookedFoodInInv() == false)
                            FindCook();
                        }
                        else
                            BecomeNobody();
                    }

                    if (Cultivator)
                        Debug.Log("нет такой профессии");

                    if (Grazier)
                        Debug.Log("нет такой профессии");

                    if (FarmExp >= 12 && IsAdult && FarmSkill <= 1)
                        FarmSkill++;
                }

                if (Mason && !SearchingFood && !IsWorking) // Mason
                {
                    if (Miner)
                        FindStone();

                    if (Bricklayer)
                        Debug.Log("нет такой профессии");

                    if (StoneBuilder)
                        Debug.Log("нет такой профессии");

                    if (Sculptor)
                        Debug.Log("нет такой профессии");


                    MasonExp += 0.1f;// *TimeMinutepassed;    
                    if (MasonExp >= 12 && IsAdult && MasonSkill <= 1)
                        MasonSkill++;
                }

                if (Warrior && !SearchingFood && !IsWorking) // Warrior
                {
                    if (Sentinel && !SearchingFood && !IsWorking)
                        Debug.Log("нет такой профессии");

                    if (Hunter && !SearchingFood && !IsWorking)
                        Debug.Log("нет такой профессии");

                    if (Marshal && !SearchingFood && !IsWorking)
                        Debug.Log("нет такой профессии");

                    BecomeNobody(); // Это потом надо удалить!
                    // Потому что это сбрасывает профессию, а сбрасываем мы их тут, потому что их сейчас нет!
                }

                if (BlackSmith && !SearchingFood && !IsWorking) // BlackSmith
                {
                    if (CraftMaster && !SearchingFood && !IsWorking) //CraftMaster
                        Debug.Log("нет такой профессии");

                    if (ToolMaster && !SearchingFood && !IsWorking)
                        Debug.Log("нет такой профессии");

                    if (Tailor && !SearchingFood && !IsWorking)
                        Debug.Log("нет такой профессии");

                    BecomeNobody(); // Это потом надо удалить!
                }

                if (Herbalist && !SearchingFood && !IsWorking) // Herbalist
                {
                    if (HerbalColletor && !SearchingFood && !IsWorking)
                        Debug.Log("нет такой профессии");

                    if (Pharmacist && !SearchingFood && !IsWorking)
                        Debug.Log("нет такой профессии");

                    if (Healer && !SearchingFood && !IsWorking)
                        Debug.Log("нет такой профессии");

                    BecomeNobody(); // Это потом надо удалить!
                }

            #endregion

            }
        }

    }

    public void Selection()
    {
        //ProfessionDropDown.SetActive(true);        
        //Если выдеяем пешку, она показывает свою профессию 
        if (WoodWorker)
        {
            ChgDropDown.value = 1; // включаем дроп даун на место нашей перент професии
            Varis.CheckProf = 0; //говорим, какой лист дроп даунов подпрофессий нам нужен
        }

        if (Farmer)
        {
            ChgDropDown.value = 2;
            Varis.CheckProf = 1;
        }

        if (Mason)
        {
            ChgDropDown.value = 3;
            Varis.CheckProf = 2;
        }

        if (Warrior)
        {
            ChgDropDown.value = 4;
            Varis.CheckProf = 3;
        }

        if (BlackSmith)
        {
            ChgDropDown.value = 5;
            Varis.CheckProf = 4;
        }

        if (Herbalist)
        {
            ChgDropDown.value = 6;
            Varis.CheckProf = 5;
        }

        Varis.ActivationChar(); // Запускаем метод в vars, который отобразит нам все элементы на дисплее
    }

    public void Diselection()
    {
       // ProfessionDropDown.SetActive(false);
        Varis.DeselectionChar();

        ChgDropDown.value = 0; // снимаем выделение, убираем профессию
    }

    /// <summary>
    /// Поиск для добычи сырья древесины
    /// </summary>
    public void FindWood()
    {
       // Debug.Log("Началась сборка");

        IsWorking = true;

        int ES = CheckMassEmptySlot(Inventory); // Проверяем есть ли место в инвентаре
        bool IsNearTarget;

        if (ES <= 0) // Если места нет, отправляемся складировать на склад
        {
            FindAndGo("Chest");
           // Debug.Log("Инвентарь полон, бежим складировать" + ES);
            if (IsNearTarget = FindAndGo("Chest"))
                PutInChase();
        }

        else if (ES >= 1) // Если есть свободное место в инвентаре
        {
            // List<GameObject> Wood = new List<GameObject>();

            GameObject[] Wood_Massive = GameObject.FindGameObjectsWithTag("Wood"); // Ищем объекты с тэгом Wood и пишем в массив

            int ESW = CheckMassSlot(Wood_Massive); // Проверяем, есть ли в нашем массиве объекты
            if (ESW >= 1) // Если есть, то...
            {
                GameObject Wood = FindAndGo("Wood"); // Находим объект с тэгом "Вуд" - т.е. готовая древесина

                if (Vector3.Distance(Wood.transform.position, MyTransform.position) <= TakeDis) // И двигаемся к нему до минимальной дистанции
                {
                    for (int i = 0; i <= Inventory.Length - 1; i++) // перебираем ячейки инвентаря в поисках свободной
                    {
                        if (Inventory[i] == null) // Складируем в инвентарь, если свободно
                        {
                            PickUpick(Wood); // запускаем функцию подбора с земли
                            break;
                        }
                    }
                }
            }

            else // Если не находим готовой древесины, ищем деревья
            {
                Wood_Massive = GameObject.FindGameObjectsWithTag("Tree"); // Ищем объекты с тэгом "дерево"

                ESW = CheckMassSlot(Wood_Massive); // Проверяем, нашлись ли деревья

                if (ESW >= 1) // Если нашли хоть 1 дерево
                {
                    //FindAndGo("Tree");
                    GameObject Wood = FindAndGo("Tree"); // находим ближайшее дерево и двигаемся к нему

                    if (Vector3.Distance(Wood.transform.position, MyTransform.position) <= TakeDis + 2f) // И двигаемся к нему до минимальной дистанции
                    {
                        StartCoroutine(ChoppingWood(Wood)); // Начинаем рубить дерево
                    }

                    else
                        IsWorking = false; // закончили оперицию работы
                }

                else
                {
                    Debug.Log(gameObject.name + " - Говорит: Нет древесины рядом! - Сам(а) в шоке");
                    IsBusy = false;
                    IsWorking = false; // закончили оперицию работы
                    BecomeNobody();
                }
            }


        }

        else
        {
            IsBusy = false;
            IsWorking = false;
        }
    }

    /// <summary>
    /// Поиск для добычи сырья камня
    /// </summary>
    public void FindStone()
    {
        // Debug.Log("Началась сборка");

        IsWorking = true; // начали работу

        int ES = CheckMassEmptySlot(Inventory);
        bool IsNearTarget;

        if (ES <= 0)
        {
            FindAndGo("Chest");
            // Debug.Log("Инвентарь полон, бежим складировать" + ES);
            if (IsNearTarget = FindAndGo("Chest"))
                PutInChase();
        }

        else if (ES >= 1)
        {
            // List<GameObject> Wood = new List<GameObject>();

            GameObject[] Wood_Massive = GameObject.FindGameObjectsWithTag("Stone");

            int ESW = CheckMassSlot(Wood_Massive);
            if (ESW >= 1)
            {
                GameObject Wood = FindAndGo("Stone");

                if (Vector3.Distance(Wood.transform.position, MyTransform.position) <= TakeDis)
                {
                    for (int i = 0; i <= Inventory.Length - 1; i++)
                    {
                        if (Inventory[i] == null) // Складируем в инвентарь, если свободно
                        {
                            PickUpick(Wood);
                            break;
                        }
                    }
                }
            }

            else
            {
                Wood_Massive = GameObject.FindGameObjectsWithTag("StoneMine");

                ESW = CheckMassSlot(Wood_Massive);

                if (ESW >= 1)
                {
                    //FindAndGo("Tree");

                    GameObject Wood = FindAndGo("StoneMine");
                    if (Vector3.Distance(Wood.transform.position, MyTransform.position) <= TakeDis + 2f)
                    {                        
                            StartCoroutine(Mining(Wood));                        
                    }

                    else
                        IsWorking = false; // закончили оперицию работы
                }

                else
                {
                    Debug.Log(gameObject.name + " - Говорит: По близости нет руды!");
                    IsBusy = false;
                    IsWorking = false; // закончили работу
                    BecomeNobody();
                }
            }


        }

            //Если всё херня, то сбрасываем работу
        else
        {
            IsBusy = false;
            IsWorking = false; // закончили работу
        }
    }

    /// <summary>
    /// Поиск для добычи сырья фруктов
    /// </summary>
    public void FindFruit()
    {
        // Debug.Log("Началась сборка");

        //IsBusy = true;
        IsWorking = true; // Начали

        int IsEmptyInv = CheckMassEmptySlot(Inventory);  // Проверяем свободный ли инвентарь
        bool IsNearTarget;

        if (IsEmptyInv <= 0)
        {
            FindAndGo("Chest");
            // Debug.Log("Инвентарь полон, бежим складировать" + ES);
            if (IsNearTarget = FindAndGo("Chest"))
                PutInChase();
        }

        else if (IsEmptyInv >= 1)
        {
            // List<GameObject> Wood = new List<GameObject>();

            GameObject[] Fruit_Massive = GameObject.FindGameObjectsWithTag("Herbal");

            int IsHerbalHave = CheckMassSlot(Fruit_Massive); // Проверяем есть ли лёгкодоступная пища для вегетерианцев
            if (IsHerbalHave >= 1) // Если есть ищем
            {
                GameObject Herbal = FindAndGo("Herbal"); // Ищем ближайшую

                if (Vector3.Distance(Herbal.transform.position, MyTransform.position) > 100f) // если ближайшая далеко ищем куст
                {
                    if (Vector3.Distance(Herbal.transform.position, MyTransform.position) <= TakeDis) // Если плод перед нами, хватаем
                    {
                        for (int i = 0; i <= Inventory.Length - 1; i++) // Ищем место в инвентаре
                        {
                            if (Inventory[i] == null) // Складируем в инвентарь, если свободно
                            {
                                PickUpick(Herbal); // поднимаем
                                break;
                            }
                        }
                    }
                }

                else // Если же фрукты далековато то
                {
                    Fruit_Massive = GameObject.FindGameObjectsWithTag("FruitBush"); // ищем кусты

                    IsHerbalHave = CheckMassSlot(Fruit_Massive);

                    if (IsHerbalHave >= 1)
                    {
                        //FindAndGo("Tree");

                        GameObject Fruit = FindAndGo("FruitBush");

                        if (Vector3.Distance(Fruit.transform.position, MyTransform.position) <= TakeDis + 2f)
                        {
                            StartCoroutine(FarmFruits(Fruit));
                        }

                        else
                            IsWorking = false;
                    }
                }
            }

            else if (IsHerbalHave <= 0)
            {
                Fruit_Massive = GameObject.FindGameObjectsWithTag("FruitBush");

                IsHerbalHave = CheckMassSlot(Fruit_Massive);

                if (IsHerbalHave >= 1)
                {
                    //FindAndGo("Tree");

                    GameObject Fruit = FindAndGo("FruitBush");
                    if (Vector3.Distance(Fruit.transform.position, MyTransform.position) <= TakeDis + 3f)
                    {
                            StartCoroutine(FarmFruits(Fruit));                        
                    }

                    else
                        IsWorking = false; // закончили оперицию работы
                }

                else
                {
                    BecomeNobody();
                    Debug.Log(gameObject.name + " - Говорит: Нет фруктов и овощей по близости!");
                }
            }


        }

            //Если всё херня, то сбрасываем работу
        else
        {
            IsBusy = false;
            IsWorking = false; // закончили работу
        }
    }

    /// <summary>
    /// Поиск строящихся зданий
    /// </summary>
    public void FindBuild()
    {
        Debug.Log("Стройка началась ёпта!");
       //IsBusy = true;
        IsWorking = true; // begin работу

        GameObject[] Struct_Massive = GameObject.FindGameObjectsWithTag("Construction"); // Ищем все строящиеся здания

        int HaveObInMassive = CheckMassSlot(Struct_Massive); // Проверяем есть ли что то строящееся

        if (HaveObInMassive >= 1) //Если таковые имеются
        {
            Debug.Log("У нас есть что построить в количестве: " + HaveObInMassive);

            GameObject StructObj = FindNearObj("Construction"); // Ищем ближайший строящийся объект       //FindAndGo("Construction");  // Двигаемся к зданию            ////////////// ЭТО ГАВНО КОМЕЕНТАРНОЕ МОЖНО УДАЛИТЬ!

            ConstructionBuilding Build = StructObj.GetComponent<ConstructionBuilding>(); // ссылка на строящийся дом

            bool CanBuild = Build.CanBuilding(); // Проверка можно ли строить объект (проверка на достаточное количество ресурсов)
            Debug.Log("Можно ли начать строить!? " + CanBuild);

            if (CanBuild)  // если всё готово к постройке, то строим
            {
                if (Vector3.Distance(StructObj.transform.position, MyTransform.position) <= TakeDis && !TakingSupplies) //Если рядом то строим
                {
                    Debug.Log("Таки зашли мы в условие строительства потому что мы внутри дома");

                    if (CanBuild)  // если всё готово к постройке, то строим
                    {
                        //int Num = Convert.ToInt32(Math.Floor(WoodSkill)); // Ахуенный преобразователь к нижнему значению
                        StartCoroutine(Build.Building(WoodSkill));
                    }


                    /*
                else // Если нихера не готово, то ищем ресурсы
                {
                    bool HaveAnSuppliesStone = FindSuppliesIInv("Stone");
                    bool HaveAnSuppliesWood = FindSuppliesIInv("Wood");

                    // if (!HaveAnSuppliesStone || !HaveAnSuppliesWood)
                    //   TakingSupplies = true;

                    if (HaveAnSuppliesStone || HaveAnSuppliesWood)
                    {
                        if (Build.Stone < Build.NeedStone) // Ищем камни
                        {
                            if (Vector3.Distance(StructObj.transform.position, MyTransform.position) <= StopDis + 2f && HaveAnSuppliesStone)
                            {
                                PutInBuilding(Build, "Stone");
                            }
                        }

                        if (Build.Wood < Build.NeedWood) // Ищем Дерево
                        {
                            if (Vector3.Distance(StructObj.transform.position, MyTransform.position) <= StopDis + 2f && HaveAnSuppliesWood)
                            {
                                PutInBuilding(Build, "Wood");
                            }
                        }
                    }

                    else
                        FindAndGo("Chest");
                }//
                     */

                }

                else
                    GoToObject(StructObj);
            }                                

            else // если нихера не готово к постройке, то ищем ресурсы
            {
                Debug.Log("Чего то не хвает");

                if (Build.Stone < Build.NeedStone) // Если не хватает при строительстве ищем Камень везде
                {                    
                    String NameResourse = "Stone"; // создаём универсальную переменную с именем ресурса, который ищем
                    bool HaveAnSupplies = FindSuppliesIInv(NameResourse); // проверка на наличие объекта в инвентаре   


                    if (HaveAnSupplies == false) // Если в инвентаре ничего не нашли
                    {
                        Debug.Log("Не хватает " + NameResourse + " Ищу его везде");
                        TakingSupplies = false;
                        if (HaveAnSupplies = FindSupplies(NameResourse, Build.NeedStone - Build.Stone)) // Ищем объекты везде и двигаемся к ним
                        {
                            IsWorking = false; // закончили цикл работы, начинаем заново
                        }

                        else // Если нигде нет подходящего камня вообще
                        {
                            Debug.Log("Что-то пошло не так, нет нигде " + NameResourse);
                            IsWorking = false; // закончили цикл работы, начинаем заново
                        }
                    }

                    else // Если мы в итоге нашли нужные ресурсы
                    {
                        PutInBuilding(Build, NameResourse); // То складируем всё в него
                        IsWorking = false; // закончили цикл работы, начинаем заново
                        Debug.Log("Пытаемся сложить на стройку");
                    }
                }


                else if (Build.Wood < Build.NeedWood) // Если не хватает при строительстве ищем Камень везде
                {
                    String NameResourse = "Wood"; // создаём универсальную переменную с именем ресурса, который ищем
                    bool HaveAnSupplies = FindSuppliesIInv(NameResourse); // проверка на наличие объекта в инвентаре   


                    if (HaveAnSupplies == false) // Если в инвентаре ничего не нашли
                    {
                        Debug.Log("Не хватает " + NameResourse + " Ищу его везде");
                        TakingSupplies = false;
                        if (HaveAnSupplies = FindSupplies(NameResourse, Build.NeedWood - Build.Wood)) // Ищем объекты везде и двигаемся к ним
                        {
                            IsWorking = false; // закончили цикл работы, начинаем заново
                        }

                        else // Если нигде нет подходящего камня вообще
                        {
                            Debug.Log("Что-то пошло не так, нет нигде " + NameResourse);
                            IsWorking = false; // закончили цикл работы, начинаем заново
                        }
                    }

                    else // Если мы в итоге нашли нужные ресурсы
                    {
                        PutInBuilding(Build, NameResourse); // То складируем всё в него
                        IsWorking = false; // закончили цикл работы, начинаем заново
                        Debug.Log("Пытаемся сложить на стройку");
                    }
                }

            }

        }

        else
        {
            Debug.Log(gameObject.name + " - Говорит: Нет зданий для постройки!");
            IsBusy = false;
            IsWorking = false; // закончили работу
            BecomeNobody();
        }
    }


    /// <summary>
    /// Поиск жрачки, чтобы её приготовить
    /// </summary>
    public void FindCook()
    {
        bool IsHaveMeat = false; // Есть ли мясо?
        bool IsHaveVegetables = false; // Есть ли фрукты?
        int WhatCook = 0; // какую еду готовим 0 - никакую, 1 - мясо, 2 - фрукты
        //Проверяем есть ли еда, которую можно приготовить в инвентаре

        //Если в инветаре нет мест и нет предметов для готовки, нахер складируем всё в ящик
        if (CheckMassEmptySlot(Inventory) <= 0 && FindSuppliesIInv("Meat") == false && FindSuppliesIInv("Herbal") == false)
        {            
                PutInChase();            
        }
            //В ином случае готовим
        else
        {
            if (FindSuppliesIInv("Meat") != false)
            {
                IsHaveMeat = true;
                WhatCook = 1;
            }

            else
            {
                IsHaveMeat = FindSupplies("Meat");
            }

            if (WhatCook == 0 && FindSuppliesIInv("Herbal") != false)
            {
                IsHaveVegetables = true;
                WhatCook = 2;
            }

            else
                IsHaveVegetables = FindSupplies("Herbal");

            bool IsHaveWood = false; // Есть ли дрова?

            //Проведварительно проверяем есть ли у нас дрова, если нет ищем дрова на складе, если нет дров, тупим
            if (WhatCook >= 1)
            {
                if (FindSuppliesIInv("Wood") != false)
                    IsHaveWood = true;

                if (!IsHaveWood)
                    if (FindSupplies("Wood") != false)
                    {
                        IsHaveWood = true;
                    }

                    else
                    {
                        Debug.Log("Всё пиздец, дров нет!");
                        //Говорим, что якобы isBusy = false
                        IsBusy = false;
                        IsWorking = false; // закончили работу
                    }
            }

            // Тут мы запускаем функцию движения к костру или к месту работы, если есть дрова и еда
            if (IsHaveWood && WhatCook != 0)
            {
                GameObject Campfire = FindAndGo("Campfire");

                // Проверяем, если мы у места работы
                if (Vector3.Distance(Campfire.transform.position, MyTransform.position) <= TakeDis)
                {
                    // То запускаем функцию поджигания огня, если у нас есть дрова, то они тратятся
                    StartCoroutine(Campfire.GetComponent<Bonfire>().KindleFire());
                    // тут надо отнять дрова                

                    //После чего запускаем функцию Cook(), не забываем, что это корутин!!!
                    if (WhatCook == 1)
                        StartCoroutine(Cooking(true));

                    if (WhatCook == 2)
                        StartCoroutine(Cooking(false));
                }

                else
                {
                   // Debug.Log("Двигаемся к костру"); Хуй знает для чего
                }
            }

            else if (!IsHaveMeat && !IsHaveVegetables)
            {
                Debug.Log("здесь нет работы милорд!");
                IsBusy = false;
                IsWorking = false; // закончили работу
                PutInChase();
                BecomeNobody();
            }
        }             
    }


    /// <summary>
    /// Готовка еды, возвращает GameObject в зависимости от того, что готовилось
    /// </summary>
    public IEnumerator Cooking(bool IsMeat)
    { 
        //тут должна быть анимация готовки
        yield return new WaitForSeconds(0); // процесс готовки закончится через...

        // Проверяем, что мы только что готовили true - мясо false - Фрукты ?
        //Если готовим мясо
        if (IsMeat)
        {
            int EmtyInv = CheckMassSlot(Inventory); // Считываем количество мест в инвентаре

            if (EmtyInv >= 2) // если мест больше двух, то
            {
                for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory еду
                {
                    if (Inventory[i] != null) // Если на что то натыкаемся
                    {
                        if (Inventory[i].tag == "Meat") // Если это мясо
                        {
                            Inventory[i] = CookedMeat; //Мясо в инвентарь
                            PutInInventory(CookedMeat); // Больше мяса в инвентарь сучка!!!
                            DisplayInv();
                            break;
                        }
                    }
                }
            }
            else // Если не хватает места, кладём нахер всё
                PutInChase();
        }

        if (!IsMeat) // готовим не мясо
        {
            for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory еду
            {
                if (Inventory[i] != null) // Если на что то натыкаемся
                {
                    if (Inventory[i].tag == "Herbal") // Если это хербал
                    {
                        Inventory[i] = CookedVegetables; //Вегетаблес в инвентарь
                        DisplayInv();
                        break;
                    }
                }
            }
        }

        //Если мы всё ещё поваришки то продолжаем работать в том же духе
        if (Cook)
        {
            CheckCookedFoodInInv(); // относим приготовленную пищу
        }

        //убираем предмет go из инвентаря и меняем его на что то другое

        //Если мясо, то возвращаем gameobject CookedMeat в количестве двух раз, хуй знает как я это сделаю...

        //Если фрукты, то возвращаем суп из овощей.
            
        
    }


    /// <summary>
    /// Проверка приготовленной пищи в инвентаре и относим её если таковая имеется
    /// </summary>
    public bool CheckCookedFoodInInv()
    {
        int NumOfCook = 0; // сколько у нас приготовленной еды            

        for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory еду
        {
            if (Inventory[i] != null) // Если на что то натыкаемся
            {
                if (Inventory[i].tag == "CookedHerbal" || Inventory[i].tag == "Cooked Meat") // Если это какая-то приготовленная еда, то запоминаем сколько их.
                {
                    NumOfCook++; // зпомнили сколько
                }
            }
        }

        //Если нет приготовленной пищи то возврщаем нет
        if (NumOfCook == 0)
            return false;

        GameObject[] FoodInInv = new GameObject[NumOfCook]; // Инициализируем массив для найденой приготовленной еды

        for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory еду
        {
            if (Inventory[i] != null) // Если на что то натыкаемся
            {
                if (Inventory[i].tag == "CookedHerbal" || Inventory[i].tag == "Cooked Meat") // Если это какая-то приготовленная еда, то какая.
                {
                    for (int y = 0; y < FoodInInv.Length; y++)
                        FoodInInv[y] = Inventory[i]; // запоминаем какая
                }
            }
        }

        //Если у нас что то лежит в инвентаре приготовленное, то мы несём это на склад
        for (int i = 0; i < FoodInInv.Length; i++)
            if (FoodInInv[i] != null)
            {
                PutInChase(FoodInInv);
                break;
            }

        //Если есть пища, то говорим, что она есть
        return true;
    }


    /// <summary>
    /// Ищем указанные ингредиенты в инвентаре, возвращает значение true или false в зависимости от тога нашёл или нет
    /// </summary>
    /// <param name="TextTag"></param>
    /// <returns></returns>
    public bool FindSuppliesIInv(string TextTag)
    {
        bool Check = false;

        int ChekInv = CheckMassSlot(Inventory); // проверяем есть ли вообще элементы в инвентаре

        if (ChekInv >= 1)
        {
            for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory еду
            {
                if (Inventory[i] != null)
                {
                    if (Inventory[i].tag == TextTag)
                    {
                        Check = true;
                        break;
                    }
                }
            }
        }

        return Check;
    }


    /// <summary>
    /// Ищем указанные ингредиенты сначала в инвентаре, а в противном случае в ближайшем сундуке, возвращает значение нашёл или нет
    /// </summary>
    /// <param name="Text"></param>
    public bool FindSupplies(string TextTag)
    {
        bool Check = false;
        TakingSupplies = true;

        int ChekInv = CheckMassSlot(Inventory); // проверяем есть ли вообще элементы в инвентаре
        int CheckInventorySlots = CheckMassEmptySlot(Inventory); // Проверяем есть ли свободные места в инвентаре
                    

        if (ChekInv >= 1)
        {
            for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory
            {
                if (Inventory[i] != null)
                {
                    if (Inventory[i].tag == TextTag)
                    {
                        Check = true;
                        TakingSupplies = false;
                        break;
                    }
                }
            }
        }

        if (CheckInventorySlots <= 0) // Если мы не нашли нужный нам объект, а инвентарь нахрен полон, то бежим нахрен всё складировать
        {
            Debug.Log("Инвентарь забит бежим складировать");
            PutInChase();
        }

        if (CheckInventorySlots >= 1 && Check == false)
        {
            Check = CheckChests(TextTag, true); // Рядом ли мы стоим, если нет продолжаем двигаться            

            /* Старое
            GameObject[] Object_Massive = GameObject.FindGameObjectsWithTag("Chest"); // Ищем сундук

            int IsHaveChest = CheckMassSlot(Object_Massive);

            if (IsHaveChest >= 1)
            {
                GameObject Ch = FindAndGo("Chest");  //Ищем сундучёк

                if (Ch != null)
                {
                    if (Vector3.Distance(Ch.transform.position, MyTransform.position) < 2f) // Если рядом то забираем камни из сундука
                    {
                        Chest StoneCh = Ch.GetComponent<Chest>(); // Ссылка на ящике

                        GameObject go = StoneCh.TakeOut(TextTag);
                                             
                        PutInInventory(go);
                    }
                }             
            }
             */
        }

        return Check;
    }


    /// <summary>
    /// Ищем указанные ингредиенты в ближайшем сундуке, забираем из сундука указанное значение, возвращает значение нашёл или нет
    /// </summary>
    /// <param name="Text"></param>
    public bool FindSupplies(string TextTag, int Num)
    {
        bool Check = false;
        TakingSupplies = true;

        int ChekInv = CheckMassSlot(Inventory); // проверяем есть ли вообще элементы в инвентаре
        int CheckInventorySlots = CheckMassEmptySlot(Inventory); // Проверяем есть ли свободные места в инвентаре

        bool ShouldPutOut = false; // Переменная для проверки стоит ли нам всё складировать из инвентаря

        if (ChekInv != Inventory.Length)
        {
            // Если нам надо отнести дохрена объектов к стройке и оно превышает или равно максимальному значению нашего инвентаря или
            if (Inventory.Length <= Num)
            {
                for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory
                {
                    if (Inventory[i] != null)
                    {
                        if (Inventory[i].tag != TextTag) // Если у нас есть хоть что-то, что не надо нам отнести к стройке
                        {
                            ShouldPutOut = true; // говорим, что надо всё это сложить
                            break;
                        }
                    }
                }
            }
                // В противном случае если у нас свободных мест меньше, чем надо положить
            else if (CheckInventorySlots <= Num)
            {
                for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory
                {
                    if (Inventory[i] != null)
                    {
                        if (Inventory[i].tag != TextTag) // Если у нас есть хоть что-то, что не надо нам отнести к стройке
                        {
                            ShouldPutOut = true; // говорим, что надо всё это сложить
                            break;
                        }
                    }
                }
            }


            if (ShouldPutOut) // Если мы нашли не нужный нам объект, то бежим нахрен всё складировать
            {
                Debug.Log("Инвентарь забит бежим складировать");
                PutInChase(); // Кладём в ящик
            }
        }

        if (CheckInventorySlots >= 1 && Check == false)
        {
            Check = CheckChests(TextTag, true); // Рядом ли мы стоим, если нет продолжаем двигаться 

            CheckInventorySlots = CheckMassEmptySlot(Inventory); // Проверяем есть ли свободные места в инвентаре

            if (Num > 0 && CheckInventorySlots > 0) // Если остались ещё объекты, которые надо положить, то кладём их
            {                
                Num--;
                FindSupplies(TextTag, Num);

                Debug.Log("сколько ещё надо положить " + Num);
            }

            /* Старое
            GameObject[] Object_Massive = GameObject.FindGameObjectsWithTag("Chest"); // Ищем сундук

            int IsHaveChest = CheckMassSlot(Object_Massive);

            if (IsHaveChest >= 1)
            {
                GameObject Ch = FindAndGo("Chest");  //Ищем сундучёк

                if (Ch != null)
                {
                    if (Vector3.Distance(Ch.transform.position, MyTransform.position) < 2f) // Если рядом то забираем камни из сундука
                    {
                        Chest StoneCh = Ch.GetComponent<Chest>(); // Ссылка на ящике

                        GameObject go = StoneCh.TakeOut(TextTag);
                                             
                        PutInInventory(go);
                    }
                }             
            }
             */
        }

        return Check;
    }

    
    /// <summary>
    /// Положить в строящееся здание ресурс.
    /// </summary>
    /// <param name="StructObj"></param>
    /// <param name="ResourceTag"></param>
    public void PutInBuilding(ConstructionBuilding StructObj, string ResourceTag)
    {
        ConstructionBuilding Build = StructObj;

        if (Vector3.Distance(StructObj.transform.position, MyTransform.position) <= TakeDis)
        {
            for (int i = 0; i < Inventory.Length; i++)  //Цикл, в массиве Inventory ищем что-то
            {
                if (Inventory[i] != null)
                {
                    if (ResourceTag == "Wood")
                    {
                        if (Inventory[i].tag == ResourceTag) // Если находим камень, то кладём его на строительство!
                        {
                            Build.Wood++;
                            Inventory[i] = null;
                            DisplayInv();      // Обновляем дисплей инвентаря
                            TakingSupplies = false;
                        }
                    }

                    if (ResourceTag == "Stone")
                    {
                        if (Inventory[i].tag == ResourceTag) // Если находим камень, то кладём его на строительство!
                        {
                            Build.Stone++;
                            Inventory[i] = null;
                            DisplayInv();      // Обновляем дисплей инвентаря
                            TakingSupplies = false;
                        }
                    }
                }
            }
        }

            // Если мы не рядом со зданием то двигаемся к нему
        else
            GoToObject(StructObj.gameObject);
        
    }


    /// <summary>
    /// Складирование на склад или ближайший ящик всё содержимое инвентаря
    /// </summary>
    public void PutInChase()
    {

        //Debug.Log("Пытаемся складировать в ящик");
        //IsBusy = true;
        IsBringing = true; // bringing
        List<GameObject> go = new List<GameObject>();

        GameObject[] Object_Massive = GameObject.FindGameObjectsWithTag("Chest"); // ищем сундки

        //Добавляем их в листинг
        foreach (GameObject get in Object_Massive)
            go.Add(get);

        //Сортируем по дальности
        go.Sort(delegate(GameObject t1, GameObject t2)
        {
            return Vector3.Distance(t1.transform.position, MyTransform.position).CompareTo(Vector3.Distance(t2.transform.position, MyTransform.position));
        });

        if (Vector3.Distance(go[0].transform.position, MyTransform.position) < 100f)
            movementTargetPosition = go[0].transform.position;                   //Элемент для движения

        else
        {
            Debug.Log("Всё пиздец, я заблудился(ась) и хуй знает где сундук");
        }

        if (Vector3.Distance(go[0].transform.position, MyTransform.position) <= TakeDis + 2f)
        {
            Chest CHslots;


            CHslots = go[0].GetComponent<Chest>();

            for (int i = 0; i <= Inventory.Length - 1; i++) // Складируем нахрен весь инвентарь
            {
                if (Inventory[i] != null) // Если есть что-то в ячейке, нахрен кладём в ящик
                {
                    bool Bringed = CHslots.PuttingIn(Inventory[i]); // Смотрм, есть ли свободные места в ящике, одновременно складируя в него

                    if (Bringed == true) // Если ящик говорит, что положился объект, то кладём стираем его из нашего инвентаря
                    {
                        Inventory[i] = null;

                        if (isActive)
                            DisplayInv(); // отображаем на дисплее, если активен перс
                    }

                    else
                        Debug.Log("Ящик полон мазафака!");
                }

                // Debug.Log("Количество проходов в цикле складирования" + i);
            }
                
            


            /*
             * 
             * GameObject[] CHslots;
            CHslots = go[0].GetComponent<Chest>().Slots;

            for (int i = 0; i <= Inventory.Length - 1; i++)
            {
                if (Inventory[i] != null)
                {
                    for (int j = 0; j <= CHslots.Length - 1; j++)
                    {
                        if (CHslots[j] == null) // Если что-то лежит в ячейке, выводи её иконку на экран
                        {
                            CHslots[j] = Inventory[i];
                            Inventory[i] = null;

                            if (isActive)
                                DisplayInv();
                           // Debug.Log("Количество сложенных предметов" + i);
                        }

                       // Debug.Log("Количество проходов в цикле складирования" + i);
                    }
                }
            }
             */
            IsBusy = false;
            IsBringing = false; // not Bringing
            IsWorking = false;
        }
    }


   /// <summary>
    /// Складирование на склад или ближайший ящик указанные предметы из инвентаря
   /// </summary>
   /// <param name="Objects"></param>
    public void PutInChase(GameObject[] Objects)
    {
        IsBringing = true; // bringing
        List<GameObject> go = new List<GameObject>();

        GameObject[] Object_Massive = GameObject.FindGameObjectsWithTag("Chest"); // ищем сундки

        //Добавляем их в листинг
        foreach (GameObject get in Object_Massive)
            go.Add(get);


        //Сортируем по дальности
        go.Sort(delegate(GameObject t1, GameObject t2)
        {
            return Vector3.Distance(t1.transform.position, MyTransform.position).CompareTo(Vector3.Distance(t2.transform.position, MyTransform.position));
        });

        if (Vector3.Distance(go[0].transform.position, MyTransform.position) < 100f)
            movementTargetPosition = go[0].transform.position;                   //Элемент для движения

        else
        {
            Debug.Log("Всё пиздец, я заблудился(ась) и хуй знает где сундук");
        }

        //Если мы рядом, то кладём туда объект
        if (Vector3.Distance(go[0].transform.position, MyTransform.position) <= TakeDis + 2f)
        {
            Chest CHslots;

            CHslots = go[0].GetComponent<Chest>();

            for (int i = 0; i <= Inventory.Length - 1; i++) // просматриваем весь инвентарь
            {
                if (Inventory[i] != null) // Если есть что-то в ячейке, сравниваем с тем что должны положить
                {
                    for (int y = 0; y < Objects.Length; y++) // сравниваем данный объект со всеми объектами
                    {
                        if (Inventory[i].gameObject.tag == Objects[y].tag) // если объект подходит
                        {
                            bool Bringed = CHslots.PuttingIn(Inventory[i]); // Смотрм, есть ли свободные места в ящике, одновременно складируя в него

                            if (Bringed == true) // Если ящик говорит, что положился объект, то кладём стираем его из нашего инвентаря
                            {
                                Inventory[i] = null;

                                if (isActive)
                                    DisplayInv(); // отображаем на дисплее, если активен перс
                            }

                            else
                                Debug.Log("Ящик полон мазафака!");
                        }
                    }
                }

                // Debug.Log("Количество проходов в цикле складирования" + i);
            }

        }
        

             IsBusy = false;
            IsBringing = false; // not Bringing
    }

    /// <summary>
    /// рубка дерева
    /// </summary>
    /// <param name="Wood"></param>
    /// <returns></returns>
    public IEnumerator ChoppingWood(GameObject Wood)
    {
        //IsBusy = true;
        IsWorking = true; // begin работу

        Anim.SetBool("ChopLow", true);

        yield return new WaitForSeconds(ClocksGame.MinuteFormula * 60);

        GameObject Collected = Wood.GetComponent<Resources>().Collecting(InstrumentPoint + WoodSkill); // Колотим обхект и получаем с него
        //плюшки в зависимости от того 

        if (Collected != null)
        {
            for (int i = 0; i <= Inventory.Length - 1; i++)  //Цикл, ищем в массиве свободные места
            {
                //Debug.Log("Началось: " + i);
                if (Inventory[i] == null) //&& Inventory.Length >= i)
                {
                    Inventory[i] = Collected; //когда находим записываем в него заранее заготовленный объект

                    if (isActive)
                        DisplayInv();

                    break;
                }
            }
        }

        Anim.SetBool("ChopLow", false);
        IsBusy = false;
        IsWorking = false; // end работу
    }


    /// <summary>
    /// Добыча камня
    /// </summary>
    /// <param name="Wood"></param>
    /// <returns></returns>
    public IEnumerator Mining(GameObject Stone)
    {
        //IsBusy = true;
        IsWorking = true;

        Anim.SetBool("ChopLow", true);

        yield return new WaitForSeconds(1.25f);

        GameObject Collected = Stone.GetComponent<Resources>().Collecting(InstrumentPoint + MasonSkill); // Колотим обхект и получаем с него
        //плюшки в зависимости от того 

        if (Collected != null)
        {
            for (int i = 0; i <= Inventory.Length - 1; i++)  //Цикл, ищем в массиве свободные места
            {
                //Debug.Log("Началось: " + i);
                if (Inventory[i] == null) //&& Inventory.Length >= i)
                {
                    Inventory[i] = Collected; //когда находим записываем в него заранее заготовленный объект

                    if (isActive)
                        DisplayInv();

                    break;
                }
            }
        }

        Anim.SetBool("ChopLow", false);
        IsBusy = false;
        IsWorking = false; // end работу
    }


    /// <summary>
    /// сбор фруктов
    /// </summary>
    /// <param name="Fruits"></param>
    /// <returns></returns>
    public IEnumerator FarmFruits(GameObject Fruits)
    {
        //IsBusy = true;
        IsWorking = true; // begin работу

        Anim.SetBool("Grab", true);

        yield return new WaitForSeconds(ClocksGame.MinuteFormula * 60);

        
        
            GameObject Collected = Fruits.GetComponent<Resources>().Collecting(InstrumentPoint);

            if (Collected != null)
            {
                //ГАВНОКОД!!! ВОЗМОЖНО ЭТО НАДО БУДЕТ ПЕРЕДЕЛАТЬ!!! НО ПОКА ЭТО РАБОТАЕТ ПУСЧАЙ ОСТАНЕТСЯ... :,(
                for (int Бизнес = 1; Бизнес <= 2; Бизнес++)                
                for (int i = 0; i <= Inventory.Length - 1; i++)  //Цикл, ищем в массиве свободные места
                {
                    //Debug.Log("Началось: " + i);
                    if (Inventory[i] == null) //&& Inventory.Length >= i)
                    {
                        Inventory[i] = Collected; //когда находим записываем в него заранее заготовленный объект

                        if (isActive)
                            DisplayInv();

                        break;
                    }
                }
            
        }

        Anim.SetBool("Grab", false);
        IsBusy = false;
        IsWorking = false; // end работу
        Farming = false;
    }

    /// <summary>
    /// Дебаф на ночь, все спелы выше 1 уровня теряют 1 значение. Или снимаем дебаф со всего через !IsOn
    /// </summary>
    /// <param name="IsOn"></param>
    public void NightDebuf(bool IsOn)// ВНИМАНИЕ НЕ ПРОПИСАН INSTRMENTPOINT !!!!!!!!!!!!!!!! НАДО ЛИ ЕГО ТРОГАТЬ?????
    {
        if (IsOn) // дебафаем
        {
            if (WoodSkill >= 1)
            WoodSkill -= 1;

            if (FarmSkill >= 1)
            FarmSkill -= 1;

            if (MasonSkill >= 1)
            MasonSkill -= 1;

            if (WarSkill >= 1)
            WarSkill -= 1;

            if (BlackSkill >= 1)
            BlackSkill -= 1;

            if (HerbalistSkill >= 1)
            HerbalistSkill -= 1;
        }

        if (!IsOn)
        {
            WoodSkill += 1;
            FarmSkill += 1;
            MasonSkill += 1;
            WarSkill += 1;
            BlackSkill += 1;
            HerbalistSkill += 1;
        }
    }


    /// <summary>
    /// Ищем ближайший объект по тэгу и возвращаем его значение GameObject
    /// </summary>
    /// <param name="Mass"></param>
    /// <returns></returns>
    public GameObject FindNearObj(string NameSearchObject)
    {
        GameObject[] Object_Massive = GameObject.FindGameObjectsWithTag(NameSearchObject);

        if (Object_Massive.Length == 0)
        {
            return null;            
        }

        List<GameObject> go = new List<GameObject>();

        foreach (GameObject get in Object_Massive)
        {
            go.Add(get);
        }

        go.Sort(delegate(GameObject t1, GameObject t2)
        {
            return Vector3.Distance(t1.transform.position, MyTransform.position).CompareTo(Vector3.Distance(t2.transform.position, MyTransform.position));
        });

        if (Vector3.Distance(go[0].transform.position, MyTransform.position) < 100f)
        {
            return go[0];            
        }

        else
        {
            Debug.Log(gameObject.name + " - Говорит: Всё писец, я заблудился(ась) и хрен знает где объект");
            return null;
        }
    }


    /// <summary>
    /// Поиск места для сна
    /// </summary>
    public void FindPlaceToSleep()
    {
        Debug.Log("Ищем место поспать");
        GameObject SleepPlace = FindNearObj("House");

        if (SleepPlace != null)
        {
            Building NearBudiling = SleepPlace.GetComponent<Building>(); // ссылка на объект ближайшего дома

            GameObject go = NearBudiling.DoorOfHouse; // получаем ссылку на дверь дома

            if (Vector3.Distance(go.transform.position, MyTransform.position) <= TakeDis) // если мы не в двух шагах от двери, двигаемся к ней
            {
                Debug.Log(gameObject.name + " говорит: Я иду к двери");
                movementTargetPosition = go.transform.position;                   // Назначаем цель передвижения
            }

            else // если мы у двери, то мы её открываем
            {
                Debug.Log(gameObject.name + " говорит: Я пытаюсь открыть дверь");
                go.GetComponent<Animator>().SetBool("IsOpen", true);

                //movementTargetPosition = NearBudiling.InsideHouse.transform.position;
                GoToSleep(NearBudiling.InsideHouse.transform.position);
            }
        }

        else
        {
            Debug.Log("Ложимся у костра");

            SleepPlace = FindNearObj("Campfire");
            if (SleepPlace != null)
            {
                FindAndGo("Campfire");

                if (Vector3.Distance(SleepPlace.transform.position, MyTransform.position) <= TakeDis + 1f && !Walk)
                {
                    // Если темно, то жжём
                    if (NightPhase)
                        StartCoroutine(SleepPlace.GetComponent<Bonfire>().KindleFire()); // Зажигаем огонь
                    //Кладёмся спать
                    StartSleepDay = Vars.DayNum; // запоминаем день в котором легли спать
                    StartSleepHour = Vars.DayTime; // и время
                    IsAsleep = true; //  Засыпаем
                }
            }
        }
    }

    /// <summary>
    /// Идём к месту сна, ложимся спать по указанному месту.
    /// </summary>
    /// <param name="Target"></param>
    public void GoToSleep(Vector3 Target)
    {
        movementTargetPosition = Target;

        if (Vector3.Distance(Target, MyTransform.position) <= TakeDis + 1f)
        {
            //Кладёмся спать
            StatesStandartization(); // Скидываем всё
            IsAsleep = true; //  Засыпаем            
            movementTargetPosition = MyTransform.position; // Чтоб если вдруг что не перемещаться к точке сна снова, а лежать, где уснул
            StartSleepDay = Vars.DayNum; // запоминаем день в котором легли спать
            StartSleepHour = Vars.DayTime; // и время
            
        }
    }


    /// <summary>
    /// Скидываем все состояния на нет
    /// </summary>
    public void StatesStandartization()
    {
        TakingSupplies = false;
        Run = false;
        IsWorking = false;
        IsNearWater = false;
        PlaceIsBusy = false;
        IsBringing = false;
        IsIdling = false;
        IsFighting = false;
        SearchingFood = false;
        Drinking = false;
    }
    
    /// <summary>
    /// Изменяем все дропдауны в соответствии с уровнями профессии выделенного персонажа
    /// </summary>
    public void ChangeDispDropDown()
    {
        // При выделении (активации) пешки, перезаписываем в дроп дауне все уровни профессий.
        ChgDropDown.options[1].text = "WoodWorker " + WoodSkill;
        ChgDropDown.options[2].text = "Farmer  " + FarmSkill;
        ChgDropDown.options[3].text = "Mason  " + MasonSkill;
        ChgDropDown.options[4].text = "Warrior  " + WarSkill;
        ChgDropDown.options[5].text = "BlackSmith  " + BlackSkill;
        ChgDropDown.options[6].text = "Herbalist  " + HerbalistSkill;        
    }


    /// <summary>
    /// Обнуляем все профы и скидываем, сброс, если профессия запрещает работу по какой то причине, dropdown приводится к nobody
    /// </summary>
    public void BecomeNobody()
    {
        WoodWorker = false; Farmer = false; Mason = false; Warrior = false; BlackSmith = false; Herbalist = false; // Пэрент Профессия
        Woodcutter = false; Builder = false; Carpenter = false; Forester = false;                                 // Дочерняя профа WoodWorker
        FoodCollector = false; Cook = false; Cultivator = false; Grazier = false;                                // Дочерняя профа Farmer
        Miner = false; Bricklayer = false; StoneBuilder = false;                                                // Дочерняя профа Mason
        Sentinel = false; Hunter = false; Marshal = false;                                                     // Дочерняя профа Warrior
        CraftMaster = false; ToolMaster = false; Tailor = false;                                              // Дочерняя профа BlackSkill
        HerbalColletor = false; Pharmacist = false; Healer = false;                                          // Дочерняя профа HerbalistSkill 

        ChgDropDown.value = 0;
    }


    /// <summary>
    /// Сброс профессий для смены профессии 
    /// </summary>
    public void BecomeNobodyForChange()
    {
        WoodWorker = false; Farmer = false; Mason = false; Warrior = false; BlackSmith = false; Herbalist = false; // Пэрент Профессия
        Woodcutter = false; Builder = false; Carpenter = false; Forester = false;                                 // Дочерняя профа WoodWorker
        FoodCollector = false; Cook = false; Cultivator = false; Grazier = false;                                // Дочерняя профа Farmer
        Miner = false; Bricklayer = false; StoneBuilder = false; Sculptor = false;                              // Дочерняя профа Mason
        Sentinel = false; Hunter = false; Marshal = false;                                                     // Дочерняя профа Warrior
        CraftMaster = false; ToolMaster = false; Tailor = false;                                              // Дочерняя профа BlackSkill
        HerbalColletor = false; Pharmacist = false; Healer = false;                                          // Дочерняя профа HerbalistSkill 
    }


    /// <summary>
    /// Сбрасываем сразу все чилдрены, чтобы не писать 100 раз всё это
    /// </summary>
    public void BecomeNobodyChildren()
    {
        Woodcutter = false; Builder = false; Carpenter = false; Forester = false;                                 // Дочерняя профа WoodWorker
        FoodCollector = false; Cook = false; Cultivator = false; Grazier = false;                                // Дочерняя профа Farmer
        Miner = false; Bricklayer = false; StoneBuilder = false; Sculptor = false;                              // Дочерняя профа Mason
        Sentinel = false; Hunter = false; Marshal = false;                                                     // Дочерняя профа Warrior
        CraftMaster = false; ToolMaster = false; Tailor = false;                                              // Дочерняя профа BlackSkill
        HerbalColletor = false; Pharmacist = false; Healer = false;                                          // Дочерняя профа HerbalistSkill 
    }
}






