using System.Collections.Generic;
using UnityEngine;

/// Reaproveita objetos (inimigos e projeteis) em vez de Instantiate/Destroy a cada um (issue #19).
///
/// Get no lugar de Instantiate, Release no lugar de Destroy. Release deixa o objeto desativado
/// (OnDisable roda) e Get o reativa (OnEnable roda), entao quem usa Pool precisa zerar o proprio
/// estado ao ser reaproveitado: o Enemy faz isso em Init, o Projectile tambem.
/// Objeto que nao veio de Get (ex.: instanciado direto num teste) e destruido de verdade em Release.
///
/// O estado vive num objeto "Pool" na cena. Ao trocar de cena ele e destruido junto com os
/// inimigos e projeteis guardados, e o proximo Get recomeca do zero.
public static class Pool
{
    static GameObject root;
    static readonly Dictionary<GameObject, Stack<GameObject>> livres = new Dictionary<GameObject, Stack<GameObject>>();
    static readonly Dictionary<GameObject, GameObject> origem = new Dictionary<GameObject, GameObject>();   // instancia -> prefab

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        EnsureRoot();
        if (livres.TryGetValue(prefab, out Stack<GameObject> pilha))
        {
            while (pilha.Count > 0)
            {
                GameObject go = pilha.Pop();
                if (go == null) continue;
                go.transform.SetParent(null);
                go.transform.SetPositionAndRotation(position, rotation);
                go.SetActive(true);
                return go;
            }
        }

        GameObject novo = Object.Instantiate(prefab, position, rotation);
        origem[novo] = prefab;
        return novo;
    }

    /// Prefab de onde esta instancia saiu, ou null se nao veio de Get (ex.: instanciada direto num teste).
    public static GameObject PrefabOf(GameObject go) =>
        go != null && origem.TryGetValue(go, out GameObject prefab) ? prefab : null;

    public static void Release(GameObject go)
    {
        if (go == null) return;
        if (root == null || !origem.TryGetValue(go, out GameObject prefab))
        {
            Object.Destroy(go);
            return;
        }
        if (!go.activeSelf) return;   // ja devolvido

        go.SetActive(false);
        go.transform.SetParent(root.transform);
        if (!livres.TryGetValue(prefab, out Stack<GameObject> pilha))
            livres[prefab] = pilha = new Stack<GameObject>();
        pilha.Push(go);
    }

    static void EnsureRoot()
    {
        if (root != null) return;
        livres.Clear();
        origem.Clear();
        root = new GameObject("Pool");
    }
}
