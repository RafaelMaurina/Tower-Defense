# Arte do HUD superior v2

As dez imagens em `Assets/_Project/Resources/hud/v2` foram criadas com a ferramenta integrada de geração de imagens. As imagens do menu e as primeiras peças deste conjunto serviram como referência de estilo. Os PNGs originais, com transparência, permanecem nessa pasta; o `GameHUD` recorta as margens ao exibi-los no Unity.

## Prompts usados

**Moldura de recursos:** Handmade pixel art for the farming tower-defense game Defesa da Colheita, warm oak wood, deliberate pixel clusters, dark brown center, cream highlights, small wheat and leaf ornaments. One long horizontal wooden frame with a continuous readable center and ornate corners. Transparent background. No text, numbers, icons or internal dividers. Match the existing menu button.

**Placa de ação:** One compact horizontal wooden action button matching the resource frame: warm oak rim, dark reddish-brown center, chunky hand-drawn corner joints, tiny wheat or green leaf details at the ends. Front view, transparent background, no text or icons.

**Base quadrada:** One square oak-wood control frame with blank dark reddish-brown center for a dynamic icon. Deliberate Aseprite-style pixel clusters, light beveled border, small iron fasteners and subtle green leaves. Front view, transparent background, no icon or text.

**Ícones isolados:** For each icon, generate one large centered transparent PNG in the same handmade pixel-art palette, with deliberate clusters and a dark-brown outline, legible at 48×48 pixels. Subjects: red heart (`icon_life`), golden coin with wheat (`icon_coins`), golden wheat ear (`icon_rounds`), two cream-gold vertical bars (`icon_pause`), green play triangle (`icon_play`), simple golden gear (`icon_settings`), two green right-pointing arrows (`icon_auto`). No button frame, text, numbers, scene or extra objects.

Em todos os prompts: evitar pintura suave, gradientes, aparência vetorial, 3D, borrão, sombras externas e margens transparentes grandes. Os estados selecionado e indisponível são aplicados pelo Unity à arte base, para manter tamanho e alinhamento idênticos.
