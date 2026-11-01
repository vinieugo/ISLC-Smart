# Custom ISLC Pro ⚡
> **Advanced Windows Memory, GPU Scheduling & Frame Delivery Optimization Suite**
> *(Suite Avançada de Otimização de Memória, Agendamento de GPU e Entrega de Quadros para Windows)*

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-0078D6?style=for-the-badge&logo=windows" alt="Platform Windows" />
  <img src="https://img.shields.io/badge/.NET%20Framework-4.8-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 4.8" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License" />
  <img src="https://img.shields.io/badge/Architecture-Pure%20Native%20NT%20APIs-blueviolet?style=for-the-badge" alt="Native APIs" />
</p>

---

## 🌐 Idioma / Language

- [🇧🇷 **Versão em Português**](#-versão-em-português)
- [🇺🇸 **English Version**](#-english-version)

---

# 🇧🇷 Versão em Português

## 📋 Visão Geral

O **Custom ISLC Pro** é uma evolução de alto desempenho inspirada no consagrado conceito do *Intelligent Standby List Cleaner (ISLC)* de Wagnardsoft. Desenvolvido em C# nativo (.NET Framework 4.8 / x64), ele foi inteiramente reprojetado para ir muito além de um simples limpador de memória RAM em segundo plano: trata-se de um **otimizador completo de latência, frametime e agendamento de processos/GPU** para jogos e aplicações de alta demanda no Windows 10 e Windows 11.

Ele utiliza chamadas nativas de baixo nível ao kernel do Windows NT (`ntdll.dll`, `kernel32.dll`, `psapi.dll`, `advapi32.dll`) com elevação de privilégios (`SeProfileSingleProcessPrivilege`, `SeIncreaseQuotaPrivilege`, `SeIncreaseBasePriorityPrivilege`).

---

## ⚔️ Diferenciais: Custom ISLC Pro vs. ISLC Convencional

A tabela abaixo detalha as principais diferenças arquiteturais e práticas entre o ISLC convencional (Wagnardsoft) e o **Custom ISLC Pro**:

| Recurso / Funcionalidade | ISLC Convencional (Wagnardsoft) | Custom ISLC Pro ⚡ |
| :--- | :---: | :---: |
| **Estratégia de Limpeza de Standby** | Purga agressiva e cega (`MemoryPurgeStandbyList`). Esvazia todo o cache indiscriminadamente. | **Smart Low-Priority Purge (`MemoryPurgeLowPriorityStandbyList`)**: Descarta apenas páginas de baixa prioridade (0 a 4), preservando o cache quente dos arquivos do jogo na RAM e evitando micro-engasgos (stuttering). |
| **Esvaziamento de Páginas Modificadas** | ❌ Não possui | ✅ **Flush Modified List (`MemoryFlushModifiedList`)**: Grava páginas modificadas em disco antes da purga para evitar picos repentinos de I/O de disco. |
| **Auto-Boost do Jogo em Foco** | ❌ Não possui | ✅ **Foreground Process Auto-Boost**: Monitora em tempo real a janela ativa e eleva prioridade de CPU para *High*, liga *PriorityBoost*, prioridade de página para nível 5 e prioridade de I/O para nível 2. Restaura o estado normal dinamicamente ao sair do jogo. |
| **Anti-Power Throttling (EcoQoS)** | ❌ Sem suporte | ✅ Desativa dinamicamente o *Process Power Throttling* (`PROCESS_POWER_THROTTLING_EXECUTION_SPEED`) na API nativa do Windows para impedir quedas de frequência (downclocking) da CPU no meio de cenas pesadas. |
| **Otimização MMCSS & Agendador de GPU** | ❌ Não possui | ✅ Ajusta a política multimídia do sistema (`Multimedia\SystemProfile`): `SystemResponsiveness = 0` (100% dos ciclos dedicados ao jogo em tempo real), desativa `NetworkThrottling`, eleva `GPU Priority = 8` e `SFIO Priority = High`. |
| **Eliminação de Lag do GameDVR** | ❌ Não possui | ✅ Desativa capturas em segundo plano do GameDVR / DWM swapchain que causam micro-travamentos na renderização. |
| **Plano de Energia Ultimate Performance** | ❌ Não possui | ✅ Habilita e ativa nativamente o esquema oculto de **Desempenho Máximo (Ultimate Performance)** do Windows via `powercfg`, eliminando o *Core Parking* e latências de C-States. |
| **Limpeza de Working Set de Apps Segundo Plano** | ❌ Esvazia tudo sem distinção ou não possui | ✅ **Empty Process Working Sets Inteligente**: Limpa o consumo inútil de memória de processos secundários, **protegendo automaticamente o jogo em foco** e respeitando uma **Lista de Exclusão** personalizada (Discord, OBS, etc.). |
| **Condições Lógicas de Gatilho** | Apenas `AND` fixo | ✅ Suporte a operadores lógicos flexíveis: **`AND`** (Ambas as condições) ou **`OR`** (Qualquer uma das condições). |
| **Resolução de Timer do Windows** | Resolução fixa | ✅ Controle em tempo real da resolução do timer com suporte a **0.50 ms** e leitura direta via `NtQueryTimerResolution` / `NtSetTimerResolution`. |
| **Interface Visual & Usabilidade** | Interface clássica WinForms cinza anos 90 | ✅ **Interface Dark Mode moderna**, barra gráfica segmentada em tempo real com distribuição de páginas (`MemoryBarControl`), badges coloridos e design limpo. |
| **Atalho Global (Hotkey)** | ❌ Não possui | ✅ Tecla de atalho global configurável (ex: `Ctrl + Alt + P`) para forçar a limpeza instantânea sem precisar sair do jogo com Alt+Tab. |
| **Suporte Multilíngue (i18n)** | Apenas inglês fixo | ✅ **Trilíngue nativo**: Suporte completo a **Inglês (Padrão)**, **Português (Brasil)** e **Russo (Русский)** com alternância dinâmica in-app e persistência no `config.json`. |
| **Linha de Comando & Automação** | Parâmetros básicos | ✅ Suporte completo a flags: `-purge`, `-runonce`, `-polling <ms>`, `-listsize <MB>`, `-freememory <MB>`, `-lang <en|pt-BR|ru>`, `-minimized`. |
| **Portabilidade** | Requer extração/instalador | ✅ Executável único compilado nativamente x64, standalone, sem dependências extras ou telemetria. |

---

## ✨ Principais Recursos em Detalhes

### 1. 🧠 Smart Low-Priority Purge (Limpeza Seletiva)
No ISLC tradicional, a limpeza convencional apaga todo o cache de espera (Standby List). Isso pode causar o efeito colateral conhecido como *cache-miss stutter*: o jogo precisa recarregar texturas e assets do SSD/HDD logo após a limpeza. 
O Custom ISLC Pro implementa a purga de **baixa prioridade (0 a 4)**, mantendo os arquivos mais acessados (prioridades 5 a 7) intactos na memória e liberando apenas o que é lixo inerte.

### 2. 🚀 Foreground Auto-Boost & Desativação do EcoQoS
Ao alternar entre janelas, o aplicativo identifica o jogo em execução e:
- Define prioridade de CPU para `High` e ativa o `PriorityBoost`;
- Seta a prioridade de I/O para nível 2 e memória para nível 5;
- Desativa o estrangulamento de energia (*ProcessPowerThrottling* / EcoQoS do Windows), garantindo que os núcleos da CPU mantenham seus clocks máximos;
- Ao dar Alt+Tab ou fechar o jogo, restaura os parâmetros do processo para a normalidade sem poluir o sistema.

### 3. 🎮 Otimização Multimídia MMCSS & Agendador da GPU
O agendador multimídia do Windows (MMCSS) reserva por padrão 20% do processamento do sistema para tarefas de segundo plano. O Custom ISLC Pro ajusta `SystemResponsiveness` para `0` (100% focado no jogo), seta a prioridade de GPU para o nível máximo `8` no perfil *Games* e desativa a retenção de pacotes de rede (`NetworkThrottlingIndex = 0xFFFFFFFF`).

### 4. 🧹 Working Set Cleaner com Proteção ao Jogo
Permite recuperar centenas de megabytes ou gigabytes de RAM ocupados por navegadores, launchers e utilitários em segundo plano (`EmptyWorkingSet`), garantindo que o processo do seu jogo ativo nunca seja afetado e permitindo adicionar processos protegidos à lista de exclusão.

### 5. ⏱️ Temporizador de Alta Precisão (0.5ms)
Força o relógio interno do sistema para a taxa máxima de atualização (0.5 milissegundos), reduzindo o atraso de polling de periféricos (mouse/teclado) e aumentando a consistência de entrega dos quadros.

---

## 🚀 Como Usar

1. Baixe a versão mais recente em [Releases](https://github.com/vinieugo/ISLC-Smart/releases);
2. Extraia o `CustomISLC.exe` em uma pasta de sua preferência;
3. Execute o programa como **Administrador** (necessário para aplicar os ajustes de kernel, timer e privilégios de processo);
4. Configure os limites desejados (Recomendação: Standby >= 1024 MB e Memória Livre < metade da sua RAM instalada);
5. Mantenha marcadas as opções:
   - `[x] Smart Low-Priority Purge (Preserva Cache do Jogo)`
   - `[x] Auto-Boost do Jogo em Foco (CPU High + No Throttling)`
   - `[x] Otimizar MMCSS, Agendador GPU & Desativar GameDVR`
   - `[x] Ativar Resolução Customizada (0.50 ms)`
6. Clique em **Iniciar Monitoramento**. O programa funcionará discretamente na bandeja do sistema (System Tray).

---

## ⚙️ Parâmetros de Linha de Comando (CLI)

O programa pode ser integrado a inicializadores de jogos, atalhos do Windows ou scripts (.bat / .ps1):

```bash
# Executa a purga da lista de espera silenciosamente e encerra imediatamente:
CustomISLC.exe -purge

# Verifica as regras do config.json, purga se as condições forem atendidas e encerra:
CustomISLC.exe -runonce

# Inicia minimizado na bandeja do sistema com monitoramento ativo:
CustomISLC.exe -minimized

# Inicia definindo limites personalizados via linha de comando:
CustomISLC.exe -listsize 2048 -freememory 8192 -polling 500 -lang en
```

---

## 🛠️ Como Compilar o Código Fonte

O projeto foi projetado para ser 100% independente de IDEs pesadas. Você pode compilá-lo com o compilador nativo do Windows:

1. Abra o prompt de comando ou execute o arquivo `compilar.bat`:
```cmd
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /platform:x64 /optimize+ /utf8output /win32manifest:app.manifest /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /out:CustomISLC.exe Program.cs NativeMethods.cs Config.cs MainForm.cs
```
2. Ou abra a solução no Visual Studio 2019/2022 e compile como **Release x64**.

---

## ⚖️ Direitos Autorais e Licença

Este projeto é distribuído sob os termos da licença [MIT](LICENSE).

- **Copyright (c) 2026 Custom ISLC Project & Colaboradores.**
- **Crédito e Reconhecimento:** O conceito original do *Intelligent Standby List Cleaner (ISLC)* foi concebido por **Wagnardsoft** ([wagnardsoft.com](https://www.wagnardsoft.com)). Este projeto é uma recriação independente e estendida pela comunidade, criada com o propósito de aprimorar a entrega de quadros e fornecer controle granular de hardware e memória.
- **Aviso:** Custom ISLC Pro não possui afiliação oficial, patrocínio ou vínculo com a Wagnardsoft ou com a Microsoft Corporation. Windows® é marca registrada da Microsoft Corporation.

---
---

# 🇺🇸 English Version

## 📋 Overview

**Custom ISLC Pro** is an advanced, high-performance evolution inspired by the acclaimed concept of Wagnardsoft's *Intelligent Standby List Cleaner (ISLC)*. Written in native 64-bit C# (.NET Framework 4.8), it has been completely re-engineered to transcend basic RAM cleaning: it serves as a **comprehensive latency, frametime consistency, and process/GPU scheduling optimization suite** tailored for competitive gaming and demanding workloads on Windows 10 and Windows 11.

It leverages undocumented and low-level Windows NT kernel APIs (`ntdll.dll`, `kernel32.dll`, `psapi.dll`, `advapi32.dll`) backed by explicit privilege adjustments (`SeProfileSingleProcessPrivilege`, `SeIncreaseQuotaPrivilege`, `SeIncreaseBasePriorityPrivilege`).

---

## ⚔️ Key Differences: Custom ISLC Pro vs. Conventional ISLC

| Feature / Capability | Conventional ISLC (Wagnardsoft) | Custom ISLC Pro ⚡ |
| :--- | :---: | :---: |
| **Standby Purge Strategy** | Aggressive, blind flush (`MemoryPurgeStandbyList`). Wipes the entire standby cache indiscriminately. | **Smart Low-Priority Purge (`MemoryPurgeLowPriorityStandbyList`)**: Discards only low-priority pages (0 to 4), keeping warm game assets cached in RAM to prevent disk micro-stuttering. |
| **Flush Modified Memory Pages** | ❌ Not available | ✅ **Flush Modified List (`MemoryFlushModifiedList`)**: Flushes dirty modified pages to storage prior to purging, preventing sudden disk I/O spikes. |
| **Active Foreground Process Boost** | ❌ Not available | ✅ **Foreground Auto-Boost**: Tracks active window in real-time, elevating CPU priority to *High*, enabling *PriorityBoost*, Page Priority to level 5, and I/O Priority to level 2. Gracefully reverts to normal when switching tasks. |
| **Anti-Power Throttling (EcoQoS)** | ❌ Not supported | ✅ Explicitly disables Windows 10/11 *Process Power Throttling* (`PROCESS_POWER_THROTTLING_EXECUTION_SPEED`) via native NT calls to prevent CPU frequency drops during intensive rendering scenes. |
| **MMCSS & GPU Scheduling Tweaks** | ❌ Not available | ✅ Direct tuning of system multimedia profile (`Multimedia\SystemProfile`): sets `SystemResponsiveness = 0` (100% time dedicated to games), disables `NetworkThrottling`, sets `GPU Priority = 8` and `SFIO Priority = High`. |
| **GameDVR Swapchain Lag Elimination**| ❌ Not available | ✅ Automatically disables GameDVR / background game capture hooks that cause DWM swapchain jitter and micro-stutters. |
| **Ultimate Performance Power Plan** | ❌ Not available | ✅ Automatically creates and applies Windows' hidden **Ultimate Performance** power scheme via `powercfg`, eliminating core parking and downclock latency. |
| **Background Working Set Purging** | ❌ Blind trim or not available | ✅ **Smart Working Set Cleaner**: Trims inactive working sets from bloatware while **automatically protecting the active game PID** and respecting a configurable **Exclusion List** (Discord, OBS, etc.). |
| **Trigger Logic Condition** | Fixed `AND` only | ✅ Flexible logical operators: Choose between **`AND`** (Both conditions met) or **`OR`** (Either condition met). |
| **Windows Timer Resolution** | Static resolution | ✅ Real-time sub-millisecond timer resolution down to **0.50 ms** queried and driven via `NtQueryTimerResolution` / `NtSetTimerResolution`. |
| **User Interface & UX** | Traditional 90s-style gray WinForms | ✅ **Modern Dark Theme**, live segmented memory distribution graphic bar (`MemoryBarControl`), real-time metric cards, and clean typography. |
| **Global Hotkey** | ❌ Not supported | ✅ User-definable global shortcut (e.g. `Ctrl + Alt + P`) to trigger instantaneous purging in-game without Alt-Tabbing. |
| **Multi-Language Support (i18n)** | Hardcoded English only | ✅ **Native Trilingual Suite**: Full runtime localization in **English (Default)**, **Portuguese (Brazil)**, and **Russian (Русский)** with seamless on-the-fly UI switching and `config.json` persistence. |
| **Command Line / Automation** | Basic launch arguments | ✅ Rich CLI support: `-purge`, `-runonce`, `-polling <ms>`, `-listsize <MB>`, `-freememory <MB>`, `-lang <en|pt-BR|ru>`, `-minimized`. |
| **Portability & Footprint** | Installer / multi-file | ✅ Standalone x64 single binary, no bloat, no telemetry, zero unnecessary runtime dependencies. |

---

## ✨ Feature Deep Dive

### 1. 🧠 Smart Low-Priority Purge
Traditional cleaners flush the entire standby list. When high-priority cached data is purged, games are forced to re-read textures and models from disk, introducing the very frametime spikes players seek to avoid. Custom ISLC Pro purges **priorities 0 through 4**, keeping active game cache warm in high-speed RAM while recycling truly idle memory.

### 2. 🚀 Foreground Auto-Boost & Power Throttling Bypass
Whenever you focus on a game:
- CPU Priority is promoted to `High` with `PriorityBoostEnabled`;
- I/O Priority is promoted to `High` (2) and Page Priority to maximum app level (5);
- Windows Power Throttling / EcoQoS is bypassed so processor cores are never throttled during gameplay;
- Once you minimize or close the game, priority classes are safely restored.

### 3. 🎮 Multimedia Class Scheduler (MMCSS) & GPU Priority
Windows by default reserves 20% of CPU responsiveness for non-multimedia background services. Custom ISLC Pro configures `SystemResponsiveness` to `0`, sets `GPU Priority` to `8` in the `Games` profile, and disables network packet throttling for seamless packet delivery.

### 4. 🧹 Protected Working Set Trimmer
Background applications like web browsers and launchers tend to hoard physical memory. Custom ISLC Pro cleans non-essential working sets while automatically shielding the foreground game and user-whitelisted processes.

### 5. ⏱️ Ultra-Low Timer Resolution (0.50 ms)
Decreases the default Windows OS timer resolution from ~15.6 ms to 0.5 ms, yielding crisper mouse input polling, reduced input latency, and smoother frametimes.

---

## 🚀 Getting Started

1. Download the latest binary from [Releases](https://github.com/vinieugo/ISLC-Smart/releases);
2. Place `CustomISLC.exe` in any folder;
3. Run as **Administrator** (required for low-level NT kernel privileges, power schemes, and timer adjustments);
4. Set your desired thresholds (e.g., Standby >= 1024 MB and Free Memory < half of your total RAM);
5. Recommended options:
   - `[x] Smart Low-Priority Purge (Preserves Game Cache)`
   - `[x] Foreground Process Auto-Boost (CPU High + No Throttling)`
   - `[x] Optimize MMCSS, GPU Scheduler & Disable GameDVR`
   - `[x] Enable Custom Timer Resolution (0.50 ms)`
6. Click **Start Monitoring**. The application will minimize smoothly to the Windows System Tray.

---

## ⚙️ Command-Line Arguments (CLI)

Easily trigger or automate Custom ISLC Pro via scripts or launchers:

```bash
# Purge standby list immediately and exit:
CustomISLC.exe -purge

# Evaluate config.json rules once, purge if thresholds are exceeded, and exit:
CustomISLC.exe -runonce

# Launch minimized to tray with monitoring active:
CustomISLC.exe -minimized

# Launch with custom thresholds:
CustomISLC.exe -listsize 2048 -freememory 8192 -polling 500 -lang en
```

---

## 🛠️ Building from Source

Build effortlessly without heavy dependencies:

1. Use the included `compilar.bat` or run:
```cmd
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /platform:x64 /optimize+ /utf8output /win32manifest:app.manifest /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll /out:CustomISLC.exe Program.cs NativeMethods.cs Config.cs MainForm.cs
```
2. Or open the project in Visual Studio 2019/2022 and build for **Release x64**.

---

## ⚖️ Copyright & License

This project is licensed under the [MIT License](LICENSE).

- **Copyright (c) 2026 Custom ISLC Project & Contributors.**
- **Attribution & Acknowledgments:** The original concept of the *Intelligent Standby List Cleaner (ISLC)* was pioneered by **Wagnardsoft** ([wagnardsoft.com](https://www.wagnardsoft.com)). Custom ISLC Pro is an independent, expanded community reimplementation engineered to provide advanced frametime consistency and granular hardware control.
- **Disclaimer:** Custom ISLC Pro is not affiliated with, sponsored by, or officially endorsed by Wagnardsoft or Microsoft Corporation. Windows® is a registered trademark of Microsoft Corporation.

