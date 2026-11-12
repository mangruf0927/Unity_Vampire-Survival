using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class DataManager
{
    public static DataManager Instance { get; } = new();
    public bool IsLoaded { get; private set; }
    public int SelectedPlayerId { get; private set; } = 1;
    public PlayerData SelectedPlayer => Players.Get(SelectedPlayerId);

    public void SelectPlayer(int id)
    {
        Players.Get(id);
        SelectedPlayerId = id;
    }

    private readonly DataTable<EnemyData> enemies = new();
    private readonly DataTable<WeaponData> weapons = new();
    private readonly DataTable<AccessoryData> accessories = new();
    private readonly DataTable<PlayerData> players = new();
    private readonly DataTable<ExperienceGemData> gems = new();
    private readonly Dictionary<string, Action<JToken>> tableLoaderDictionary = new();

    public DataTable<EnemyData> Enemies { get { LoadAll(); return enemies; } }
    public DataTable<WeaponData> Weapons { get { LoadAll(); return weapons; } }
    public DataTable<AccessoryData> Accessories { get { LoadAll(); return accessories; } }
    public DataTable<PlayerData> Players { get { LoadAll(); return players; } }
    public DataTable<ExperienceGemData> Gems { get { LoadAll(); return gems; } }

    private DataManager()
    {
        tableLoaderDictionary.Add("Enemies", enemies.LoadFromToken);
        tableLoaderDictionary.Add("Weapons", weapons.LoadFromToken);
        tableLoaderDictionary.Add("Accessories", accessories.LoadFromToken);
        tableLoaderDictionary.Add("Players", players.LoadFromToken);
        tableLoaderDictionary.Add("ExperienceGems", gems.LoadFromToken);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        Instance.SelectedPlayerId = 1;
        Instance.IsLoaded = false;
        Instance.ClearTables();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadBeforeScene()
    {
        Instance.LoadAll();
    }

    public void LoadAll()
    {
        if (IsLoaded) return;
        HashSet<string> loadedTables = new();
        ClearTables();
        string currentFile = "Resources/JSON";
        try
        {
            TextAsset[] textAssets = JsonDataLoader.LoadAll("JSON");
            if (textAssets.Length == 0) throw new InvalidOperationException("JSON 파일이 없습니다.");
            for (int i = 0; i < textAssets.Length; i++)
            {
                TextAsset asset = textAssets[i];
                currentFile = asset.name;
                BaseGameData data = JsonConvert.DeserializeObject<BaseGameData>(asset.text);
                if (data == null || string.IsNullOrEmpty(data.tableName) || !(data.records is JArray))
                    throw new InvalidOperationException("tableName과 records 배열이 필요합니다.");
                if (!tableLoaderDictionary.TryGetValue(data.tableName, out Action<JToken> loader))
                    throw new InvalidOperationException($"등록되지 않은 테이블: {data.tableName}");
                if (!loadedTables.Add(data.tableName))
                    throw new InvalidOperationException($"중복 테이블: {data.tableName}");
                loader(data.records);
            }
            foreach (string tableName in tableLoaderDictionary.Keys)
                if (!loadedTables.Contains(tableName))
                    throw new InvalidOperationException($"필수 테이블 누락: {tableName}");
            currentFile = "Players";
            foreach (PlayerData player in players.All.Values)
                if (!weapons.TryGet(player.weaponId, out _))
                    throw new InvalidOperationException($"Player ID {player.Id}: 시작 무기 ID {player.weaponId}가 없습니다.");
            IsLoaded = true;
        }
        catch (Exception exception)
        {
            ClearTables();
            throw new InvalidOperationException($"게임 데이터 로드 실패 ({currentFile}): {exception.Message}", exception);
        }
    }

    private void ClearTables()
    {
        enemies.Clear();
        weapons.Clear();
        accessories.Clear();
        players.Clear();
        gems.Clear();
    }
}
