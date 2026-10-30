using System.Collections.Generic;

/// Lista dos inimigos vivos. A torre consulta aqui em vez de usar Find.
/// O proprio Enemy se registra em OnEnable e sai em OnDisable (funciona com pooling).
public static class EnemyRegistry
{
    static readonly List<Enemy> vivos = new List<Enemy>();

    public static IReadOnlyList<Enemy> All => vivos;

    internal static void Register(Enemy e)
    {
        if (!vivos.Contains(e)) vivos.Add(e);
    }

    internal static void Unregister(Enemy e) => vivos.Remove(e);
}
