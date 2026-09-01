# Guia de Engenharia Reversa do Castlevania: SotN no Ghidra

Todos os 185 binários executáveis e overlays de *Castlevania: Symphony of the Night* foram extraídos diretamente do disco para esta pasta (`ghidra_files/`).

---

## 📁 Principais Arquivos para Análise

| Arquivo | Descrição | Base Address (RAM) |
| :--- | :--- | :--- |
| **`SLUS_000.67.exe`** | **Executável Principal do PSX** (boot, kernel, drivers de vídeo, áudio, carregador) | `0x80010000` |
| **`DRA.BIN`** | **Motor Principal do Jogo** (física do Alucard, itens, magias, menus, inventário) | `0x800A0000` |
| **`BIN/RIC.BIN`** | **Modo Richter Belmont** (movimentação, chicote, sub-armas do Richter) | `0x8013C000` |
| **`ST/NO0/NO0.BIN`** | **Marble Gallery** (Estágio) | `0x80180000` |
| **`ST/LIB/LIB.BIN`** | **Long Library** (Livraria e Bibliotecário) | `0x80180000` |
| **`BOSS/BO0/BO0.BIN`** | **Chefe Olrox** | `0x80180000` |
| **`BOSS/BO6/BO6.BIN`** | **Luta contra Richter / Shaft** | `0x80180000` |

*O mapeamento completo de todos os 185 arquivos está registrado em [`ghidra_memory_map.json`](./ghidra_memory_map.json).*

---

## 🛠️ Passo a Passo para Carregar no Ghidra

### 1. Importar o Executável Principal (`SLUS_000.67.exe`)
1. No Ghidra, crie um novo projeto (*File -> New Project -> Non-Shared Project*).
2. Pressione **`I`** (*File -> Import File*) e selecione `ghidra_files/SLUS_000.67.exe`.
3. Na janela de importação:
   - Se estiver com o plugin **ghidra_psx_ldr**: Escolha o formato `PlayStation Executable`.
   - Se for importação padrão: Escolha `Raw Binary`, Linguagem `MIPS:LE:32:default` (MIPS Little-Endian 32-bit), e em *Options* defina o **Base Address** como `0x80010000`.
4. Abra o arquivo no **CodeBrowser** (ícone do dragão verde).
5. O Ghidra perguntará se deseja rodar o **Auto-Analyze**. Clique em **Yes**.

### 2. Adicionar o Motor do Jogo (`DRA.BIN`) como Overlay
Como o SotN carrega `DRA.BIN` em uma região fixa da RAM durante a gameplay:
1. Com o `SLUS_000.67` aberto no CodeBrowser, vá em **File -> Add to Program...**
2. Selecione o arquivo `ghidra_files/DRA.BIN`.
3. Na janela de opções que se abre:
   - Marque a caixa **`[x] Overlay`**.
   - Em **Block Name**, digite `DRA`.
   - Em **Base Address**, digite `0x800A0000`.
4. Vá em **Tools -> Memory Map**, selecione o bloco `DRA` e garanta que as permissões **R** (Read), **W** (Write) e **X** (Execute) estejam marcadas.
5. Selecione **Analysis -> Auto Analyze...** para o Ghidra descompilar as funções do `DRA.BIN`.

### 3. Adicionar o Richter Belmont (`BIN/RIC.BIN`) como Overlay
- Repita o processo acima selecionando `ghidra_files/BIN/RIC.BIN`.
- Marque **`[x] Overlay`**, Block Name `RIC`, Base Address `0x8013C000`.

### 4. Adicionar Estágios e Chefes (`ST/` e `BOSS/`)
- Todos os arquivos dentro de `ST/` (ex: `ST/NO0/NO0.BIN`) e `BOSS/` (ex: `BOSS/BO0/BO0.BIN`) devem ser adicionados com **Base Address `0x80180000`** e marcados como **Overlay**.

---

## 🤖 Como Usar com o Ghidra MCP
Quando você iniciar o servidor **Ghidra MCP** no seu Ghidra, ele exporá as ferramentas de inspeção de memória, descompilação C em tempo real e renomeação de símbolos via protocolo MCP, permitindo que analisemos funções MIPS e criemos novos patches e mods juntos!
