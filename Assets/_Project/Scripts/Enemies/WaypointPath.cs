using UnityEngine;

/// Caminho que os inimigos percorrem. Os pontos sao os filhos deste objeto,
/// na ordem da hierarquia: o primeiro e o spawn, o ultimo e a base.
///
/// A posicao ao longo do caminho e dada por DISTANCIA percorrida, nao pelo
/// indice do ponto. Isso deixa trivial o que vem depois no plano: o empurrao
/// do forcado (diminuir a distancia), o salto do gafanhoto (somar distancia)
/// e a copia do AFN nascendo exatamente onde o original estava.
public class WaypointPath : MonoBehaviour
{
    Vector3[] pontos;
    float[] acumulado;   // distancia do inicio ate cada ponto

    void Awake() => Rebuild();

    /// Recalcula a partir dos filhos. Chamar se os pontos mudarem em jogo.
    public void Rebuild()
    {
        int n = transform.childCount;
        pontos = new Vector3[n];
        acumulado = new float[n];
        for (int i = 0; i < n; i++)
        {
            pontos[i] = transform.GetChild(i).position;
            acumulado[i] = i == 0 ? 0f : acumulado[i - 1] + Vector3.Distance(pontos[i - 1], pontos[i]);
        }
    }

    /// Comprimento total, em unidades de mundo.
    public float Length
    {
        get
        {
            if (pontos == null) Rebuild();
            return pontos.Length == 0 ? 0f : acumulado[pontos.Length - 1];
        }
    }

    /// Posicao no caminho depois de percorrer 'distance' unidades desde o spawn.
    public Vector3 PointAt(float distance)
    {
        if (pontos == null) Rebuild();
        if (pontos.Length == 0) return transform.position;
        if (distance <= 0f) return pontos[0];

        for (int i = 1; i < pontos.Length; i++)
        {
            if (distance <= acumulado[i])
            {
                float trecho = acumulado[i] - acumulado[i - 1];
                float t = trecho > 0f ? (distance - acumulado[i - 1]) / trecho : 1f;
                return Vector3.Lerp(pontos[i - 1], pontos[i], t);
            }
        }
        return pontos[pontos.Length - 1];
    }

    // Desenha o caminho na Scene view: vermelho no spawn, verde na base.
    void OnDrawGizmos()
    {
        int n = transform.childCount;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = transform.GetChild(i).position;
            Gizmos.color = i == 0 ? Color.red : i == n - 1 ? Color.green : new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawWireSphere(p, i == 0 || i == n - 1 ? 0.3f : 0.12f);
            if (i > 0)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f);
                Gizmos.DrawLine(transform.GetChild(i - 1).position, p);
            }
        }
    }
}
