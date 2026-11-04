using UnityEngine;

/// Ordem de desenho dos objetos "em pe" (inimigos, torres). E a mesma formula do
/// ARTE.md e do gerador de niveis (MontarNiveis.Sprite_): quem esta mais embaixo na
/// tela desenha por cima. Chao fica em 0 e coisas deitadas (canteiro, plantacao) em 1.
public static class DrawOrder
{
    /// Altura do mapa em casas. Igual a H em MontarNiveis.cs (la e codigo de editor,
    /// entao nao da para compartilhar a constante); se o mapa mudar, mudar nos dois.
    public const int MapHeight = 17;

    /// Projetil voa por cima de tudo, para nunca sumir atras da cerca ou do poco.
    public const int Projectile = 500;

    public static int ForY(float y) => 100 + Mathf.RoundToInt((MapHeight - y) * 10f);
}
