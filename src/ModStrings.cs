using System;

namespace OC2ControllerIcons
{
    // Mod texts in the 12 languages supported by the game (SupportedLanguages).
    public static class ModStrings
    {
        // Order: English, French, Italian, German, Spanish, Russian, Brazilian,
        //        Polish, Chinese, Japanese, Korean, ChineseTraditional
        private static readonly string[] s_enabledTitle =
        {
            "Universal controller icons",
            "Icônes de manette universelles",
            "Icone universali del controller",
            "Universelle Controller-Symbole",
            "Iconos de mando universales",
            "Универсальные значки геймпада",
            "Ícones de controle universais",
            "Uniwersalne ikony kontrolera",
            "通用手柄图标",
            "汎用コントローラーアイコン",
            "범용 컨트롤러 아이콘",
            "通用控制器圖示",
        };

        // {0} = player number
        private static readonly string[] s_playerTitle =
        {
            "Player {0} controller",
            "Manette du joueur {0}",
            "Controller del giocatore {0}",
            "Controller von Spieler {0}",
            "Mando del Jugador {0}",
            "Геймпад игрока {0}",
            "Controle do Jogador {0}",
            "Kontroler gracza {0}",
            "玩家{0}手柄",
            "プレイヤー{0}のコントローラー",
            "플레이어 {0} 컨트롤러",
            "玩家{0}控制器",
        };

        private static readonly string[][] s_offOn =
        {
            new string[] { "Off", "On" },
            new string[] { "Non", "Oui" },
            new string[] { "No", "Sì" },
            new string[] { "Aus", "An" },
            new string[] { "No", "Sí" },
            new string[] { "Выкл.", "Вкл." },
            new string[] { "Não", "Sim" },
            new string[] { "Wył.", "Wł." },
            new string[] { "关", "开" },
            new string[] { "オフ", "オン" },
            new string[] { "끄기", "켜기" },
            new string[] { "關", "開" },
        };

        private static readonly string[] s_sectionTitle =
        {
            "CONTROLLER LAYOUTS", "DISPOSITIONS DE MANETTE", "LAYOUT DEL CONTROLLER", "CONTROLLER-LAYOUTS",
            "DISPOSICIÓN DE MANDOS", "РАСКЛАДКИ ГЕЙМПАДОВ", "LAYOUTS DE CONTROLE", "UKŁADY KONTROLERÓW",
            "手柄布局", "コントローラー配置", "컨트롤러 배치", "控制器配置",
        };

        private static readonly string[] s_generic =
        {
            "Generic", "Générique", "Generico", "Generisch", "Genérico", "Универсальный",
            "Genérico", "Ogólny", "通用", "汎用", "범용", "通用",
        };

        private static int LanguageIndex()
        {
            try
            {
                int i = (int)Localization.GetLanguage();
                return i >= 0 && i < s_enabledTitle.Length ? i : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string SectionTitle { get { return s_sectionTitle[LanguageIndex()]; } }
        public static string EnabledTitle { get { return s_enabledTitle[LanguageIndex()]; } }
        public static string[] OffOn { get { return s_offOn[LanguageIndex()]; } }
        // Same order as PadLayout.
        public static string[] Layouts
        {
            get { return new string[] { "Xbox", "PlayStation", "Nintendo", s_generic[LanguageIndex()] }; }
        }

        public static string PlayerTitle(int playerNumber)
        {
            return string.Format(s_playerTitle[LanguageIndex()], playerNumber);
        }
    }
}
