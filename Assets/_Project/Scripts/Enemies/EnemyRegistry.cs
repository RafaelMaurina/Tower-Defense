using System.Collections.Generic;

/// Lista dos inimigos vivos. A torre consulta aqui em vez de usar Find.
/// O proprio Enemy se registra em OnEnable e sai ao morrer, chegar na base ou
/// ser desativado (funciona com pooling).
///
/// A lista e a mesma durante o jogo todo: nao modificar enquanto se percorre All.
/// Quem causa dano (projetil) faz isso fora do loop da torre, entao nao ha problema.
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
