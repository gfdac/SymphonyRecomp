# 🤖 Instruções para IAs & Desenvolvedores: Como Manter e Atualizar o Website do SymphonyRecomp

Este documento serve como **guia mandatório** para você (agente de IA ou desenvolvedor) sempre que uma nova funcionalidade, patch, shader ou modo de jogo for adicionado ao repositório `SymphonyRecomp`.

---

## 📌 1. Regra Fundamental
Sempre que você criar ou modificar uma funcionalidade no código C# (.NET 10) do `SymphonyRecomp`:
1. **Adicione a feature correspondente no website** (`website/index.html`).
2. Se houver novos controles ou atalhos, **adicione uma linha na tabela de atalhos** (`#shortcutsTable`).
3. Se houver novas opções de multiplayer ou QoL, **atualize as tags de evolução** na seção de créditos.

---

## 🛠️ 2. Como Adicionar um Novo Card de Recurso

Abra [`website/index.html`](file:///c:/github/SymphonyRecomp/website/index.html) e localize a seção `<div class="features-grid">`. Insira o novo card no seguinte padrão:

```html
<!-- NOVO RECURSO: [NOME_DO_RECURSO] -->
<div class="feature-card">
    <div class="fc-badge fc-badge-gold">[TAG / CATEGORIA]</div>
    <div class="fc-icon"><i class="fa-solid fa-[ICONE_FONTAWESOME]"></i></div>
    <h3 class="fc-title">[Título do Recurso]</h3>
    <p class="fc-desc">
        [Descrição clara e empolgante do que a funcionalidade faz e como impacta o gameplay.]
    </p>
    <ul class="fc-list">
        <li><i class="fa-solid fa-check"></i> [Destaque 1]</li>
        <li><i class="fa-solid fa-check"></i> [Destaque 2]</li>
        <li><i class="fa-solid fa-check"></i> [Destaque 3]</li>
    </ul>
</div>
```

### Paleta de Cores de Badges Disponíveis (`fc-badge-*`):
- `fc-badge-danger`: Vermelho Carmesim (Desafio / Hardcore)
- `fc-badge-gold`: Dourado (Troféus / Conquistas)
- `fc-badge-purple`: Roxo Gótico (Chefes / Magia)
- `fc-badge-cyan`: Ciano (Agilidade / Movimento)
- `fc-badge-blue`: Azul (Bestiário / Informação)
- `fc-badge-magic`: Rosa Mágico (Feitiços / Spells)
- `fc-badge-retro`: Laranja CRT (Shaders / Vídeo)
- `fc-badge-green`: Verde Esmeralda (Navegação / QoL)

---

## ⌨️ 3. Como Adicionar um Novo Atalho no Cheatsheet

Na tabela `<table class="shortcuts-table" id="shortcutsTable">`, adicione uma linha `<tr>`:

```html
<tr>
    <td><strong>[Nome da Ação]</strong></td>
    <td><kbd>[Tecla Teclado]</kbd></td>
    <td><kbd>[Botão Controle]</kbd> <em>(Detalhe)</em></td>
    <td>[O que o atalho faz no jogo].</td>
</tr>
```

---

## 🎨 4. Diretrizes de Design & Estilo
- **Tipografia**: Títulos usam `Cinzel` (Gótico medieval), corpo usa `Outfit` (Moderno e legível).
- **Sem Placeholders**: Nunca use imagens cinzas ou textos "Lorem Ipsum". Use ilustrações reais ou ícones do FontAwesome.
- **Micro-interações**: Mantenha as animações suaves em hover (`var(--transition)`).

---

## 🚀 5. Como Testar o Website Localmente
O website é 100% estático (HTML5, Vanilla CSS3 e JavaScript nativo).
Basta abrir o arquivo `website/index.html` em qualquer navegador web moderno (Edge, Chrome, Firefox) ou usar uma extensão Live Server.
