using System;
using System.Collections.Generic;

namespace CustomISLC
{
    public static class Localization
    {
        public const string LANG_EN = "en";
        public const string LANG_PT_BR = "pt-BR";
        public const string LANG_RU = "ru";

        public static string CurrentLanguage { get; private set; }

        static Localization()
        {
            CurrentLanguage = LANG_EN; // Default is English
        }

        public static void SetLanguage(string lang)
        {
            if (string.Equals(lang, LANG_PT_BR, StringComparison.OrdinalIgnoreCase))
                CurrentLanguage = LANG_PT_BR;
            else if (string.Equals(lang, LANG_RU, StringComparison.OrdinalIgnoreCase))
                CurrentLanguage = LANG_RU;
            else
                CurrentLanguage = LANG_EN;
        }

        public static string Get(string key)
        {
            Dictionary<string, string> dict;
            if (_translations.TryGetValue(CurrentLanguage, out dict))
            {
                string val;
                if (dict.TryGetValue(key, out val))
                    return val;
            }

            // Fallback to English
            string fallbackVal;
            if (_translations[LANG_EN].TryGetValue(key, out fallbackVal))
                return fallbackVal;

            return key;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> _translations =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                // =========================================================================
                // ENGLISH (DEFAULT)
                // =========================================================================
                {
                    LANG_EN, new Dictionary<string, string>
                    {
                        { "AppTitle", "Custom ISLC Pro - Memory, GPU & Frame Delivery Optimizer" },
                        { "AdminSuffix", " [Administrator]" },
                        { "NoAdminSuffix", " [No Admin Rights]" },

                        // Panel 1: Memory
                        { "SectionMemoryTitle", "REAL-TIME SYSTEM MEMORY STATUS" },
                        { "StatusRunning", "● RUNNING" },
                        { "StatusStopped", "● STOPPED" },
                        { "MetricTotalMemory", "Total Memory" },
                        { "MetricStandbyList", "Standby List (Cache)" },
                        { "MetricFreeMemory", "Free Memory" },
                        { "MetricModifiedPages", "Modified Pages" },
                        { "MetricPurgesCount", "Purges Completed" },
                        { "StandbySubDetail", "Cold: {0}M | Hot: {1}M" },
                        { "MemoryBarLegend", "In Use: {0:N0} MB   |   Standby Cache: {1:N0} MB   |   Free: {2:N0} MB" },

                        // Panel 2: Rules
                        { "SectionRulesTitle", "AUTOMATIC PURGE RULES" },
                        { "StandbyLimitLabel", "The list size is at least:" },
                        { "TriggerConditionLabel", "Trigger condition:" },
                        { "ConditionAnd", "AND (Both conditions)" },
                        { "ConditionOr", "OR (Either condition)" },
                        { "FreeMemoryLimitLabel", "And free memory is lower than:" },
                        { "PollingRateLabel", "Polling rate:" },

                        // Panel 3: Timer
                        { "SectionTimerTitle", "WINDOWS TIMER RESOLUTION" },
                        { "CurrentTimerLabel", "Current Timer Resolution:" },
                        { "SystemTimerLimitsLabel", "System Range (Min/Max):" },
                        { "EnableCustomTimer", "Enable Custom Timer Resolution" },
                        { "DesiredResolutionLabel", "Desired Resolution (ms):" },

                        // Panel 4: Frame Delivery & GPU
                        { "SectionFrameOptTitle", "FRAME DELIVERY OPTIMIZATION, FRAMETIME & GPU PRIORITY" },
                        { "SmartLowPrioPurge", "Smart Standby Purge (Preserves hot game cache to prevent stuttering)" },
                        { "FlushModified", "Flush modified pages before purge" },
                        { "AutoBoostForeground", "Foreground Window Auto-Boost (CPU High + I/O + No Power Throttling):" },
                        { "BoostWaiting", "Waiting for focus..." },
                        { "BoostStopped", "Stopped" },
                        { "BoostDisabled", "Disabled" },
                        { "MmcssGpuTweaks", "Optimize GPU Scheduler (MMCSS Priority 8 + 100% Responsiveness + Anti-GameDVR)" },
                        { "UltimatePowerPlan", "Force Ultimate Performance Power Plan (No Core Parking)" },
                        { "ActivePlanPrefix", "Active Plan: " },
                        { "ActivePlanChecking", "Active Plan: Checking..." },
                        { "ActivePlanDefault", "Default" },

                        // Panel 5: General Options
                        { "SectionOptionsTitle", "GENERAL OPTIONS & GLOBAL HOTKEY" },
                        { "StartMinimized", "Start minimized to tray" },
                        { "AutoStartMonitoring", "Auto-start monitoring" },
                        { "MinimizeOnClose", "Minimize on close (X)" },
                        { "AlwaysOnTop", "Always on top" },
                        { "StartWithWindows", "Start with Windows (Admin)" },
                        { "DarkMode", "Dark Mode" },
                        { "HotkeyLabel", "Purge Hotkey:" },
                        { "LanguageLabel", "Language:" },

                        // Panel 6: Working Set & Exclusions
                        { "SectionWorkingSetTitle", "WORKING SET & EXCLUSION LIST" },
                        { "PurgeWorkingSetAuto", "Purge background Working Sets with Standby (protects active game)" },
                        { "AddExclusion", "+ Ignore Process" },
                        { "RemoveExclusion", "- Remove Selected" },
                        { "ExclusionPlaceholder", "process_name" },

                        // Buttons
                        { "BtnStartMonitoring", "▶ START MONITORING" },
                        { "BtnStopMonitoring", "⏹ STOP MONITORING" },
                        { "BtnPurgeStandbyNow", "⚡ PURGE STANDBY LIST NOW" },
                        { "BtnPurgeWorkingSetNow", "🧹 PURGE WORKING SETS NOW" },

                        // System Tray
                        { "TrayOpen", "Open Custom ISLC Pro" },
                        { "TrayStartMonitoring", "Start Monitoring" },
                        { "TrayStopMonitoring", "Stop Monitoring" },
                        { "TrayPurgeStandbyNow", "Purge Standby List Now" },
                        { "TrayPurgeWorkingSetNow", "Purge Working Sets Now" },
                        { "TrayExit", "Exit" },
                        { "TrayTooltip", "Custom ISLC Pro ({0})\nStandby: {1} MB | Free: {2} MB\nPurges: {3}" },
                        { "TrayStateActive", "Active" },
                        { "TrayStateStopped", "Stopped" },

                        // Messages
                        { "MsgAlreadyRunning", "Custom ISLC is already running (check system tray)." }
                    }
                },

                // =========================================================================
                // PORTUGUÊS (BRASIL)
                // =========================================================================
                {
                    LANG_PT_BR, new Dictionary<string, string>
                    {
                        { "AppTitle", "Custom ISLC Pro - Otimizador de Memória, GPU & Entrega de Quadros" },
                        { "AdminSuffix", " [Administrador]" },
                        { "NoAdminSuffix", " [Sem Privilégio Admin]" },

                        // Painel 1: Memória
                        { "SectionMemoryTitle", "STATUS DA MEMÓRIA DO SISTEMA EM TEMPO REAL" },
                        { "StatusRunning", "● MONITORANDO ATIVO" },
                        { "StatusStopped", "● MONITORAMENTO PARADO" },
                        { "MetricTotalMemory", "Memória Total" },
                        { "MetricStandbyList", "Standby List (Cache)" },
                        { "MetricFreeMemory", "Memória Livre" },
                        { "MetricModifiedPages", "Páginas Modificadas" },
                        { "MetricPurgesCount", "Limpezas Realizadas" },
                        { "StandbySubDetail", "Frio: {0}M | Quente: {1}M" },
                        { "MemoryBarLegend", "Em Uso: {0:N0} MB   |   Standby Cache: {1:N0} MB   |   Livre: {2:N0} MB" },

                        // Painel 2: Regras
                        { "SectionRulesTitle", "REGRAS DE LIMPEZA AUTOMÁTICA" },
                        { "StandbyLimitLabel", "Standby List maior ou igual a:" },
                        { "TriggerConditionLabel", "Condição lógica de disparo:" },
                        { "ConditionAnd", "E (Ambos)" },
                        { "ConditionOr", "OU (Qualquer um)" },
                        { "FreeMemoryLimitLabel", "Memória Livre menor que:" },
                        { "PollingRateLabel", "Taxa de verificação (Polling):" },

                        // Painel 3: Timer
                        { "SectionTimerTitle", "RESOLUÇÃO DO TIMER DO WINDOWS" },
                        { "CurrentTimerLabel", "Resolução Atual do Timer:" },
                        { "SystemTimerLimitsLabel", "Limites do Sistema (Min/Máx):" },
                        { "EnableCustomTimer", "Ativar Resolução de Timer Customizada" },
                        { "DesiredResolutionLabel", "Resolução Desejada (ms):" },

                        // Painel 4: Otimização de Imagem & GPU
                        { "SectionFrameOptTitle", "OTIMIZAÇÃO DE ENTREGA DE IMAGEM, FRAMETIME & PRIORIDADE DE GPU" },
                        { "SmartLowPrioPurge", "Limpeza Standby Inteligente (Preserva cache de alta prioridade do jogo para evitar micro-stutter)" },
                        { "FlushModified", "Descarregar páginas modificadas antes de limpar (Flush Modified)" },
                        { "AutoBoostForeground", "Auto-Boost Janela em Foco (CPU High + I/O + Sem Power Throttling):" },
                        { "BoostWaiting", "Aguardando foco..." },
                        { "BoostStopped", "Parado" },
                        { "BoostDisabled", "Desativado" },
                        { "MmcssGpuTweaks", "Otimizar Agendador GPU (MMCSS Priority 8 + Responsividade 100% + Anti-GameDVR)" },
                        { "UltimatePowerPlan", "Forçar Energia Desempenho Máximo (Sem Core Parking)" },
                        { "ActivePlanPrefix", "Plano Ativo: " },
                        { "ActivePlanChecking", "Plano Ativo: Verificando..." },
                        { "ActivePlanDefault", "Padrão" },

                        // Painel 5: Opções Gerais
                        { "SectionOptionsTitle", "OPÇÕES GERAIS & ATALHO GLOBAL" },
                        { "StartMinimized", "Iniciar minimizado na bandeja" },
                        { "AutoStartMonitoring", "Auto-iniciar monitoramento" },
                        { "MinimizeOnClose", "Minimizar ao fechar (X)" },
                        { "AlwaysOnTop", "Sempre no topo" },
                        { "StartWithWindows", "Iniciar com o Windows (Admin)" },
                        { "DarkMode", "Modo Escuro" },
                        { "HotkeyLabel", "Atalho Limpar:" },
                        { "LanguageLabel", "Idioma:" },

                        // Painel 6: Working Set & Exclusões
                        { "SectionWorkingSetTitle", "WORKING SET & LISTA DE EXCLUSÃO" },
                        { "PurgeWorkingSetAuto", "Limpar Working Set de fundo junto com Standby (ignora jogo ativo)" },
                        { "AddExclusion", "+ Ignorar Processo" },
                        { "RemoveExclusion", "- Remover Selecionado" },
                        { "ExclusionPlaceholder", "nome_do_jogo" },

                        // Botões
                        { "BtnStartMonitoring", "▶ INICIAR MONITORAMENTO" },
                        { "BtnStopMonitoring", "⏹ PARAR MONITORAMENTO" },
                        { "BtnPurgeStandbyNow", "⚡ LIMPAR STANDBY LIST AGORA" },
                        { "BtnPurgeWorkingSetNow", "🧹 LIMPAR WORKING SETS AGORA" },

                        // Bandeja do Sistema
                        { "TrayOpen", "Abrir Painel Custom ISLC Pro" },
                        { "TrayStartMonitoring", "Iniciar Monitoramento" },
                        { "TrayStopMonitoring", "Parar Monitoramento" },
                        { "TrayPurgeStandbyNow", "Limpar Standby List Agora" },
                        { "TrayPurgeWorkingSetNow", "Limpar Working Sets Agora" },
                        { "TrayExit", "Sair" },
                        { "TrayTooltip", "Custom ISLC Pro ({0})\nStandby: {1} MB | Livre: {2} MB\nLimpezas: {3}" },
                        { "TrayStateActive", "Ativo" },
                        { "TrayStateStopped", "Parado" },

                        // Mensagens
                        { "MsgAlreadyRunning", "O Custom ISLC já está em execução (verifique a bandeja do sistema)." }
                    }
                },

                // =========================================================================
                // РУССКИЙ (RUSSIAN)
                // =========================================================================
                {
                    LANG_RU, new Dictionary<string, string>
                    {
                        { "AppTitle", "Custom ISLC Pro - Оптимизатор памяти, GPU и доставки кадров" },
                        { "AdminSuffix", " [Администратор]" },
                        { "NoAdminSuffix", " [Без прав администратора]" },

                        // Панель 1: Память
                        { "SectionMemoryTitle", "СТАТУС СИСТЕМНОЙ ПАМЯТИ В РЕАЛЬНОМ ВРЕМЕНИ" },
                        { "StatusRunning", "● МОНИТОРИНГ АКТИВЕН" },
                        { "StatusStopped", "● МОНИТОРИНГ ОСТАНОВЛЕН" },
                        { "MetricTotalMemory", "Всего памяти" },
                        { "MetricStandbyList", "Список ожидания (Кэш)" },
                        { "MetricFreeMemory", "Свободная память" },
                        { "MetricModifiedPages", "Измененные страницы" },
                        { "MetricPurgesCount", "Выполнено очисток" },
                        { "StandbySubDetail", "Холодный: {0}М | Горячий: {1}М" },
                        { "MemoryBarLegend", "Используется: {0:N0} МБ   |   Standby Кэш: {1:N0} МБ   |   Свободно: {2:N0} МБ" },

                        // Панель 2: Правила
                        { "SectionRulesTitle", "ПРАВИЛА АВТОМАТИЧЕСКОЙ ОЧИСТКИ" },
                        { "StandbyLimitLabel", "Размер Standby List не менее:" },
                        { "TriggerConditionLabel", "Условие срабатывания:" },
                        { "ConditionAnd", "И (Оба условия)" },
                        { "ConditionOr", "ИЛИ (Любое)" },
                        { "FreeMemoryLimitLabel", "Свободной памяти меньше:" },
                        { "PollingRateLabel", "Частота проверки (Polling):" },

                        // Панель 3: Таймер
                        { "SectionTimerTitle", "РАЗРЕШЕНИЕ ТАЙМЕРА WINDOWS" },
                        { "CurrentTimerLabel", "Текущее разрешение таймера:" },
                        { "SystemTimerLimitsLabel", "Пределы системы (Мин/Макс):" },
                        { "EnableCustomTimer", "Включить кастомное разрешение таймера" },
                        { "DesiredResolutionLabel", "Желаемое разрешение (мс):" },

                        // Панель 4: Доставка кадров и GPU
                        { "SectionFrameOptTitle", "ОПТИМИЗАЦИЯ ДОСТАВКИ КАДРОВ, FRAMETIME И ПРИОРИТЕТА GPU" },
                        { "SmartLowPrioPurge", "Умная очистка Standby (сохраняет горячий кэш игры от статтеров)" },
                        { "FlushModified", "Сбросить измененные страницы перед очисткой (Flush Modified)" },
                        { "AutoBoostForeground", "Авто-буст активного окна (CPU High + I/O + Без Power Throttling):" },
                        { "BoostWaiting", "Ожидание фокуса..." },
                        { "BoostStopped", "Остановлено" },
                        { "BoostDisabled", "Отключено" },
                        { "MmcssGpuTweaks", "Оптимизация планировщика GPU (MMCSS Priority 8 + 100% Отзывчивость + Anti-GameDVR)" },
                        { "UltimatePowerPlan", "Форсировать схему 'Максимальная производительность' (Без Core Parking)" },
                        { "ActivePlanPrefix", "Активный план: " },
                        { "ActivePlanChecking", "Активный план: Проверка..." },
                        { "ActivePlanDefault", "По умолчанию" },

                        // Панель 5: Общие настройки
                        { "SectionOptionsTitle", "ОБЩИЕ НАСТРОЙКИ И ГОРЯЧИЕ КЛАВИШИ" },
                        { "StartMinimized", "Запускать свернутым в трей" },
                        { "AutoStartMonitoring", "Авто-старт мониторинга" },
                        { "MinimizeOnClose", "Сворачивать при закрытии (X)" },
                        { "AlwaysOnTop", "Поверх всех окон" },
                        { "StartWithWindows", "Запуск вместе с Windows (Admin)" },
                        { "DarkMode", "Темная тема" },
                        { "HotkeyLabel", "Горячая клавиша:" },
                        { "LanguageLabel", "Язык:" },

                        // Панель 6: Working Set и исключения
                        { "SectionWorkingSetTitle", "РАБОЧИЙ НАБОР (WORKING SET) И ИСКЛЮЧЕНИЯ" },
                        { "PurgeWorkingSetAuto", "Очищать фоновый Working Set вместе со Standby (защищает активную игру)" },
                        { "AddExclusion", "+ Исключить процесс" },
                        { "RemoveExclusion", "- Удалить выбранное" },
                        { "ExclusionPlaceholder", "имя_процесса" },

                        // Кнопки
                        { "BtnStartMonitoring", "▶ ЗАПУСТИТЬ МОНИТОРИНГ" },
                        { "BtnStopMonitoring", "⏹ ОСТАНОВИТЬ МОНИТОРИНГ" },
                        { "BtnPurgeStandbyNow", "⚡ ОЧИСТИТЬ STANDBY LIST СЕЙЧАС" },
                        { "BtnPurgeWorkingSetNow", "🧹 ОЧИСТИТЬ WORKING SET СЕЙЧАС" },

                        // Системный трей
                        { "TrayOpen", "Открыть Custom ISLC Pro" },
                        { "TrayStartMonitoring", "Запустить мониторинг" },
                        { "TrayStopMonitoring", "Остановить мониторинг" },
                        { "TrayPurgeStandbyNow", "Очистить Standby List сейчас" },
                        { "TrayPurgeWorkingSetNow", "Очистить Working Sets сейчас" },
                        { "TrayExit", "Выход" },
                        { "TrayTooltip", "Custom ISLC Pro ({0})\nStandby: {1} МБ | Свободно: {2} МБ\nОчисток: {3}" },
                        { "TrayStateActive", "Активен" },
                        { "TrayStateStopped", "Остановлен" },

                        // Сообщения
                        { "MsgAlreadyRunning", "Custom ISLC уже запущен (проверьте системный трей)." }
                    }
                }
            };
    }
}
