using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class DataTable<T> where T : class, IGameData, IValidatable
{
    private readonly Dictionary<int, T> table = new();
    public IReadOnlyDictionary<int, T> All => table;

    public void Clear()
    {
        table.Clear();
    }

    public void Load(IEnumerable<T> records)
    {
        if (records == null) throw new InvalidOperationException($"{typeof(T).Name}: records가 없습니다.");
        Dictionary<int, T> loaded = new();
        foreach (T record in records)
        {
            if (record == null || !record.Validate())
                throw new InvalidOperationException($"{typeof(T).Name}: 잘못된 능력치 또는 ID ({record?.Id}).");
            if (loaded.ContainsKey(record.Id))
                throw new InvalidOperationException($"{typeof(T).Name}: 중복 ID {record.Id}.");
            loaded.Add(record.Id, record);
        }
        if (loaded.Count == 0) throw new InvalidOperationException($"{typeof(T).Name}: 빈 테이블입니다.");
        table.Clear();
        foreach (KeyValuePair<int, T> pair in loaded) table.Add(pair.Key, pair.Value);
    }

    public void LoadFromToken(JToken records)
    {
        JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error
        });
        Load(records.ToObject<List<T>>(serializer));
    }

    public T Get(int id)
    {
        if (!table.TryGetValue(id, out T result))
            throw new InvalidOperationException($"{typeof(T).Name}: ID {id}를 찾을 수 없습니다.");
        return result;
    }

    public bool TryGet(int id, out T result)
    {
        return table.TryGetValue(id, out result);
    }
}
