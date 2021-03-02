// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LEGOMaterials
{

    public class MouldingColour
    {
        public enum Id
        {
            // Universal.
            White = 1,
            BrickYellow = 5,
            BrightRed = 21,
            BrightBlue = 23,
            BrightYellow = 24,
            Black = 26,
            DarkGreen = 28,
            ReddishBrown = 192,
            MediumStoneGrey = 194,
            DarkStoneGrey = 199,

            // Generic.
            Nougat = 18,
            BrightGreen = 37,
            Transparent = 40,
            TransparentRed = 41,
            TransparentLightBlue = 42,
            TransparentBlue = 43,
            TransparentYellow = 44,
            TransparentGreen = 48,
            TransparentFluorescentGreen = 49,
            MediumBlue = 102,
            BrightOrange = 106,
            TransparentBrown = 111,
            BrightYellowGreen = 119,
            EarthBlue = 140,
            EarthGreen = 141,
            NewDarkRed = 154,
            TransparentBrightOrange = 182,
            BrightPurple = 221,
            LightPurple = 222,
            MediumAzur = 322,
            MediumLavender = 324,

            // Special.
            DarkOrange = 38,
            TransparentFluorescentRedOrange = 47,
            BrightBluishGreen = 107,
            TransparentMediumViolet = 113,
            BrightReddishViolet = 124,
            TransparentBrightViolet = 126,
            SandBlue = 135,
            SandYellow = 138,
            SandGreen = 151,
            FlameYellowOrange = 191,
            LightRoyalBlue = 212,
            CoolYellow = 226,
            MediumLilac = 268,
            LightNougat = 283,
            WarmGold = 297,
            DarkBrown = 308,
            TransparentBrightGreen = 311,
            MediumNougat = 312,
            SilverMetallic = 315,
            TitaniumMetallic = 316,
            DarkAzur = 321,
            Aqua = 323,
            Lavender = 325,
            SpringYellowishGreen = 326,
            WhiteGlow = 329,
            OliveGreen = 330,
            VibrantCoral = 353,

            // Legacy Colours.
            Grey_Legacy = 2,
            LightYellow_Legacy = 3,
            LightGreen_Legacy = 6,
            LightReddishViolet_Legacy = 9,
            PastelBlue_Legacy = 11,
            LightOrangeBrown_Legacy = 12,
            Nature_Legacy = 20,
            MediumReddishViolet_Legacy = 22,
            EarthOrange_Legacy = 25,
            DarkGrey_Legacy = 27,
            MediumGreen_Legacy = 29,
            LightYellowishOrange_Legacy = 36,
            LightBluishViolet_Legacy = 39,
            LightBlue_Legacy = 45,
            PhosphorescentWhite_Legacy = 50,
            LightRed_Legacy = 100,
            MediumRed_Legacy = 101,
            LightGrey_Legacy = 103,
            BrightViolet_Legacy = 104,
            BrightYellowishOrange_Legacy = 105,
            EarthYellow_Legacy = 108,
            PcBlackIr_Legacy = 109,
            BrightBluishViolet_Legacy = 110,
            MediumBluishViolet_Legacy = 112,
            MediumYellowishGreen_Legacy = 115,
            MediumBluishGreen_Legacy = 116,
            LightBluishGreen_Legacy = 118,
            LightYellowishGreen_Legacy = 120,
            MediumYellowishOrange_Legacy = 121,
            BrightReddishOrange_Legacy = 123,
            LightOrange_Legacy = 125,
            Gold_Legacy = 127,
            DarkNougat_Legacy = 128,
            Silver_Legacy = 131,
            NeonOrange_Legacy = 133,
            NeonGreen_Legacy = 134,
            SandViolet_Legacy = 136,
            MediumOrange_Legacy = 137,
            Copper_Legacy = 139,
            TransparentFlourescentBlue_Legacy = 143,
            MetallicSandBlue_Legacy = 145,
            MetallicSandViolet_Legacy = 146,
            MetallicSandYellow_Legacy = 147,
            MetallicDarkGrey_Legacy = 148,
            MetallicBlack_Legacy = 149,
            MetallicLightGrey_Legacy = 150,
            SandRed_Legacy = 153,
            TransparentFlourescentYellow_Legacy = 157,
            TransparentFlourescentRed_Legacy = 158,
            GunMetallic_Legacy = 168,
            RedFlipFlop_Legacy = 176,
            YellowFlipFlop_Legacy = 178,
            SilverFlipFlop_Legacy = 179,
            Curry_Legacy = 180,
            MetallicWhite_Legacy = 183,
            MetallicBrightRed_Legacy = 184,
            MetallicBrightBlue_Legacy = 185,
            MetallicDarkGreen_Legacy = 186,
            MetallicEarthOrange_Legacy = 187,
            TinyBlue_Legacy = 188,
            ReddishGold_Legacy = 189,
            FireYellow_Legacy = 190,
            FlameReddishOrange_Legacy = 193,
            RoyalBlue_Legacy = 195,
            DarkRoyalBlue_Legacy = 196,
            BrightLilac_Legacy = 197,
            BrightReddishLilac_Legacy = 198,
            MetallicLemon_Legacy = 200,
            LightStoneGrey_Legacy = 208,
            DarkCurry_Legacy = 209,
            FadedGreen_Legacy = 210,
            Turquoise_Legacy = 211,
            MediumRoyalBlue_Legacy = 213,
            Rust_Legacy = 216,
            Brown_Legacy = 217,
            ReddishLilac_Legacy = 218,
            Lilac_Legacy = 219,
            LightLilac_Legacy = 220,
            LightPink_Legacy = 223,
            LightBrickYellow_Legacy = 224,
            WarmYellowishOrange_Legacy = 225,
            TransparentBrightYellowishGreen_Legacy = 227,
            TransparentLightBluishGreen_Legacy = 229,
            TransparentBrightPurple_Legacy = 230,
            TransparentFlameYellowishOrange_Legacy = 231,
            DoveBlue_Legacy = 232,
            LightFadedGreen_Legacy = 233,
            TransparentFireYellow_Legacy = 234,
            TransparentBrightReddishLilac_Legacy = 236,
            MediumBlue_Legacy = 269,
            TransparentReddishLilac_Legacy = 284,
            PhosphorescentGreen_Legacy = 294,
            CoolSilver_Legacy = 296,
            CoolSilverDrumLacq_Legacy = 298,
            MetalizedSilver_Legacy = 309,
            MetalizedGold_Legacy = 310,
            CopperMetallicInk_Legacy = 334,
            GoldMetallicInk_Legacy = 335,
            SilverMetallicInk_Legacy = 336,
            TitaniumMetallicInk_Legacy = 337,

            // Legacy Colours w/ Unknown Name.
            UnknownName00_Legacy = 4,
            UnknownName01_Legacy = 7,
            UnknownName02_Legacy = 8,
            UnknownName03_Legacy = 10,
            UnknownName04_Legacy = 13,
            UnknownName05_Legacy = 14,
            UnknownName06_Legacy = 15,
            UnknownName07_Legacy = 16,
            UnknownName08_Legacy = 17,
            UnknownName09_Legacy = 19,
            UnknownName10_Legacy = 122,
            UnknownName11_Legacy = 228,
            UnknownName12_Legacy = 285,
        }

        // LEGO Moulding Colour Guide 2019.03.12 + LEGO_Materials_Legacy provided by Anders Tankred Holm
        private readonly static Dictionary<Id, Color> idToColourGuide = new Dictionary<Id, Color>()
    {
        // Universal.
        { Id.White,                             new Color32(244, 244, 244, 255) },
        { Id.BrickYellow,                       new Color32(204, 185, 141, 255) },
        { Id.BrightRed,                         new Color32(180, 0, 0, 255)     },
        { Id.BrightBlue,                        new Color32(30, 90, 168, 255)   },
        { Id.BrightYellow,                      new Color32(250, 200, 10, 255)  },
        { Id.Black,                             new Color32(0, 0, 0, 255)       },
        { Id.DarkGreen,                         new Color32(0, 133, 43, 255)    },
        { Id.ReddishBrown,                      new Color32(95, 49, 9, 255)     },
        { Id.MediumStoneGrey,                   new Color32(150, 150, 150, 255) },
        { Id.DarkStoneGrey,                     new Color32(100, 100, 100, 255) },

        // Generic.
        { Id.Nougat,                            new Color32(187, 128, 90, 255)  },
        { Id.BrightGreen,                       new Color32(88, 171, 65, 255)   },
        { Id.Transparent,                       new Color32(238, 238, 238, 128) },
        { Id.TransparentRed,                    new Color32(184, 0, 0, 128)     },
        { Id.TransparentLightBlue,              new Color32(173, 221, 237, 128) },
        { Id.TransparentBlue,                   new Color32(0, 133, 184, 128)   },
        { Id.TransparentYellow,                 new Color32(255, 230, 34, 128)  },
        { Id.TransparentGreen,                  new Color32(115, 180, 100, 128) },
        { Id.TransparentFluorescentGreen,       new Color32(250, 241, 91, 128)  },
        { Id.MediumBlue,                        new Color32(115, 150, 200, 255) },
        { Id.BrightOrange,                      new Color32(214, 121, 35, 255)  },
        { Id.TransparentBrown,                  new Color32(187, 178, 158, 128) },
        { Id.BrightYellowGreen,                 new Color32(165, 202, 24, 255)  },
        { Id.EarthBlue,                         new Color32(25, 25, 50, 255)    },
        { Id.EarthGreen,                        new Color32(0, 69, 26, 255)     },
        { Id.NewDarkRed,                        new Color32(114, 0, 18, 255)    },
        { Id.TransparentBrightOrange,           new Color32(225, 141, 10, 128)  },
        { Id.BrightPurple,                      new Color32(200, 80, 155, 255)  },
        { Id.LightPurple,                       new Color32(255, 158, 205, 255) },
        { Id.MediumAzur,                        new Color32(104, 195, 226, 255) },
        { Id.MediumLavender,                    new Color32(154, 118, 174, 255) },

        // Special.
        { Id.DarkOrange,                        new Color32(145, 80, 28, 255)   },
        { Id.TransparentFluorescentRedOrange,   new Color32(203, 78, 41, 128)   },
        { Id.BrightBluishGreen,                 new Color32(0, 152, 148, 255)   },
        { Id.TransparentMediumViolet,           new Color32(253, 142, 207, 128) },
        { Id.BrightReddishViolet,               new Color32(144, 31, 118, 255)  },
        { Id.TransparentBrightViolet,           new Color32(111, 122, 184, 128) },
        { Id.SandBlue,                          new Color32(112, 129, 154, 255) },
        { Id.SandYellow,                        new Color32(137, 125, 98, 255)  },
        { Id.SandGreen,                         new Color32(112, 142, 124, 255) },
        { Id.FlameYellowOrange,                 new Color32(252, 172, 0, 255)   },
        { Id.LightRoyalBlue,                    new Color32(157, 195, 247, 255) },
        { Id.CoolYellow,                        new Color32(255, 236, 108, 255) },
        { Id.MediumLilac,                       new Color32(68, 26, 145, 255)   },
        { Id.LightNougat,                       new Color32(225, 190, 161, 255) },
        { Id.WarmGold,                          new Color32(175, 133, 34, 255)  },
        { Id.DarkBrown,                         new Color32(53, 33, 0, 255)     },
        { Id.TransparentBrightGreen,            new Color32(175, 210, 70, 128)  },
        { Id.MediumNougat,                      new Color32(170, 125, 85, 255)  },
        { Id.SilverMetallic,                    new Color32(140, 140, 140, 255) },
        { Id.TitaniumMetallic,                  new Color32(73, 70, 68, 255)    },
        { Id.DarkAzur,                          new Color32(70, 155, 195, 255)  },
        { Id.Aqua,                              new Color32(211, 242, 234, 255) },
        { Id.Lavender,                          new Color32(205, 164, 222, 255) },
        { Id.SpringYellowishGreen,              new Color32(226, 249, 154, 255) },
        { Id.WhiteGlow,                         new Color32(245, 243, 215, 255) },
        { Id.OliveGreen,                        new Color32(139, 132, 79, 255)  },
        { Id.VibrantCoral,                      new Color32(240, 109, 120, 255) },

        // Legacy.
        { Id.Grey_Legacy,                               new Color32(138, 146, 141, 255) },
        { Id.LightYellow_Legacy,                        new Color32(255, 214, 127, 255) },
        { Id.LightGreen_Legacy,                         new Color32(173, 217, 168, 255) },
        { Id.LightReddishViolet_Legacy,                 new Color32(246, 169, 187, 255) },
        { Id.PastelBlue_Legacy,                         new Color32(171, 217, 255, 255) },
        { Id.LightOrangeBrown_Legacy,                   new Color32(216, 109, 44, 255)  },
        { Id.Nature_Legacy,                             new Color32(233, 233, 233, 128) },
        { Id.MediumReddishViolet_Legacy,                new Color32(208, 80, 152, 255)  },
        { Id.EarthOrange_Legacy,                        new Color32(84, 51, 36, 255)    },
        { Id.DarkGrey_Legacy,                           new Color32(84, 89, 85, 255)    },
        { Id.MediumGreen_Legacy,                        new Color32(127, 196, 117, 255) },
        { Id.LightYellowishOrange_Legacy,               new Color32(253, 195, 131, 255) },
        { Id.LightBluishViolet_Legacy,                  new Color32(175, 190, 214, 255) },
        { Id.LightBlue_Legacy,                          new Color32(151, 203, 217, 255) },
        { Id.PhosphorescentWhite_Legacy,                new Color32(229, 223, 211, 255) },
        { Id.LightRed_Legacy,                           new Color32(249, 183, 165, 255) },
        { Id.MediumRed_Legacy,                          new Color32(240, 109, 97, 255)  },
        { Id.LightGrey_Legacy,                          new Color32(188, 180, 165, 255) },
        { Id.BrightViolet_Legacy,                       new Color32(103, 31, 161, 255)  },
        { Id.BrightYellowishOrange_Legacy,              new Color32(245, 134, 36, 255)  },
        { Id.EarthYellow_Legacy,                        new Color32(86, 71, 47, 255)    },
        { Id.PcBlackIr_Legacy,                          new Color32(0, 20, 20, 255)     },
        { Id.BrightBluishViolet_Legacy,                 new Color32(38, 70, 154, 255)   },
        { Id.MediumBluishViolet_Legacy,                 new Color32(72, 97, 172, 255)   },
        { Id.MediumYellowishGreen_Legacy,               new Color32(183, 212, 37, 255)  },
        { Id.MediumBluishGreen_Legacy,                  new Color32(0, 170, 164, 255)   },
        { Id.LightBluishGreen_Legacy,                   new Color32(156, 214, 204, 255) },
        { Id.BrightReddishOrange_Legacy,                new Color32(238, 84, 52, 255)   },
        { Id.LightYellowishGreen_Legacy,                new Color32(222, 234, 146, 255) },
        { Id.MediumYellowishOrange_Legacy,              new Color32(248, 154, 57, 255)  },
        { Id.LightOrange_Legacy,                        new Color32(249, 167, 119, 255) },
        { Id.Gold_Legacy,                               new Color32(222, 172, 102, 255) },
        { Id.DarkNougat_Legacy,                         new Color32(173, 97, 64, 255)   },
        { Id.Silver_Legacy,                             new Color32(160, 160, 160, 255) },
        { Id.NeonOrange_Legacy,                         new Color32(239, 88, 40, 255)   },
        { Id.NeonGreen_Legacy,                          new Color32(205, 221, 52, 255)  },
        { Id.SandViolet_Legacy,                         new Color32(117, 101, 125, 255) },
        { Id.MediumOrange_Legacy,                       new Color32(244, 129, 71, 255)  },
        { Id.Copper_Legacy,                             new Color32(118, 77, 59, 255)   },
        { Id.TransparentFlourescentBlue_Legacy,         new Color32(149, 189, 221, 128) },
        { Id.MetallicSandBlue_Legacy,                   new Color32(91, 117, 144, 255)  },
        { Id.MetallicSandViolet_Legacy,                 new Color32(129, 117, 144, 255) },
        { Id.MetallicSandYellow_Legacy,                 new Color32(131, 114, 79, 255)  },
        { Id.MetallicDarkGrey_Legacy,                   new Color32(72, 77, 72, 255)    },
        { Id.MetallicBlack_Legacy,                      new Color32(10, 19, 39, 255)    },
        { Id.MetallicLightGrey_Legacy,                  new Color32(152, 155, 153, 255) },
        { Id.SandRed_Legacy,                            new Color32(136, 96, 94, 255)   },
        { Id.TransparentFlourescentYellow_Legacy,       new Color32(255, 246, 92, 128)  },
        { Id.TransparentFlourescentRed_Legacy,          new Color32(241, 142, 187, 128) },
        { Id.GunMetallic_Legacy,                        new Color32(96, 86, 76, 255)    },
        { Id.RedFlipFlop_Legacy,                        new Color32(148, 81, 72, 255)   },
        { Id.YellowFlipFlop_Legacy,                     new Color32(171, 103, 58, 255)  },
        { Id.SilverFlipFlop_Legacy,                     new Color32(115, 114, 113, 255) },
        { Id.Curry_Legacy,                              new Color32(221, 152, 46, 255)  },
        { Id.MetallicWhite_Legacy,                      new Color32(246, 242, 223, 255) },
        { Id.MetallicBrightRed_Legacy,                  new Color32(214, 0, 38, 255)    },
        { Id.MetallicBrightBlue_Legacy,                 new Color32(0, 89, 163, 255)    },
        { Id.MetallicDarkGreen_Legacy,                  new Color32(0, 142, 60, 255)    },
        { Id.MetallicEarthOrange_Legacy,                new Color32(87, 57, 44, 255)    },
        { Id.TinyBlue_Legacy,                           new Color32(0, 158, 206, 255)   },
        { Id.ReddishGold_Legacy,                        new Color32(172, 130, 71, 255)  },
        { Id.FireYellow_Legacy,                         new Color32(255, 207, 11, 255)  },
        { Id.FlameReddishOrange_Legacy,                 new Color32(236, 68, 29, 255)   },
        { Id.RoyalBlue_Legacy,                          new Color32(28, 88, 167, 255)   },
        { Id.DarkRoyalBlue_Legacy,                      new Color32(14, 62, 154, 255)   },
        { Id.BrightLilac_Legacy,                        new Color32(49, 43, 135, 255)   },
        { Id.BrightReddishLilac_Legacy,                 new Color32(138, 18, 168, 255)  },
        { Id.MetallicLemon_Legacy,                      new Color32(106, 121, 68, 255)  },
        { Id.LightStoneGrey_Legacy,                     new Color32(200, 200, 200, 255) },
        { Id.DarkCurry_Legacy,                          new Color32(164, 118, 36, 255)  },
        { Id.FadedGreen_Legacy,                         new Color32(70, 138, 95, 255)   },
        { Id.Turquoise_Legacy,                          new Color32(63, 182, 169, 255)  },
        { Id.MediumRoyalBlue_Legacy,                    new Color32(71, 111, 182, 255)  },
        { Id.Rust_Legacy,                               new Color32(135, 43, 23, 255)   },
        { Id.Brown_Legacy,                              new Color32(123, 93, 65, 255)   },
        { Id.ReddishLilac_Legacy,                       new Color32(142, 85, 151, 255)  },
        { Id.Lilac_Legacy,                              new Color32(86, 78, 157, 255)   },
        { Id.LightLilac_Legacy,                         new Color32(145, 149, 202, 255) },
        { Id.LightPink_Legacy,                          new Color32(241, 120, 128, 255) },
        { Id.LightBrickYellow_Legacy,                   new Color32(243, 201, 136, 255) },
        { Id.WarmYellowishOrange_Legacy,                new Color32(250, 169, 100, 255) },
        { Id.TransparentBrightYellowishGreen_Legacy,    new Color32(201, 231, 136, 128) },
        { Id.TransparentLightBluishGreen_Legacy,        new Color32(172, 212, 222, 128) },
        { Id.TransparentBrightPurple_Legacy,            new Color32(236, 163, 201, 128) },
        { Id.TransparentFlameYellowishOrange_Legacy,    new Color32(252, 183, 109, 128) },
        { Id.DoveBlue_Legacy,                           new Color32(119, 201, 216, 255) },
        { Id.LightFadedGreen_Legacy,                    new Color32(96, 186, 118, 255)  },
        { Id.TransparentFireYellow_Legacy,              new Color32(251, 232, 144, 128) },
        { Id.TransparentBrightReddishLilac_Legacy,      new Color32(141, 115, 179, 128) },
        { Id.MediumBlue_Legacy,                         new Color32(62, 149, 182, 255)  },
        { Id.TransparentReddishLilac_Legacy,            new Color32(224, 208, 229, 128) },
        { Id.PhosphorescentGreen_Legacy,                new Color32(213, 220, 138, 128) },
        { Id.CoolSilver_Legacy,                         new Color32(173, 173, 173, 255) },
        { Id.CoolSilverDrumLacq_Legacy,                 new Color32(118, 118, 118, 255) },
        { Id.MetalizedSilver_Legacy,                    new Color32(206, 206, 206, 255) },
        { Id.MetalizedGold_Legacy,                      new Color32(223, 193, 118, 255) },
        { Id.CopperMetallicInk_Legacy,                  new Color32(116, 77, 59, 255)   },
        { Id.GoldMetallicInk_Legacy,                    new Color32(185, 149, 59, 255)  },
        { Id.SilverMetallicInk_Legacy,                  new Color32(140, 140, 140, 255) },
        { Id.TitaniumMetallicInk_Legacy,                new Color32(62, 60, 57, 255)    },

        // Legacy w/ Unknown Name
        { Id.UnknownName00_Legacy,  new Color32(242, 112, 94, 255)  },
        { Id.UnknownName01_Legacy,  new Color32(255, 133, 0, 255)   },
        { Id.UnknownName02_Legacy,  new Color32(140, 0, 255, 255)   },
        { Id.UnknownName03_Legacy,  new Color32(255, 255, 189, 128) },
        { Id.UnknownName04_Legacy,  new Color32(255, 128, 20, 255)  },
        { Id.UnknownName05_Legacy,  new Color32(120, 252, 120, 255) },
        { Id.UnknownName06_Legacy,  new Color32(255, 242, 48, 255)  },
        { Id.UnknownName07_Legacy,  new Color32(255, 135, 156, 255) },
        { Id.UnknownName08_Legacy,  new Color32(255, 148, 148, 255) },
        { Id.UnknownName09_Legacy,  new Color32(207, 138, 71, 255)  },
        { Id.UnknownName10_Legacy,  new Color32(254, 203, 152, 255) },
        { Id.UnknownName11_Legacy,  new Color32(85, 165, 175, 128)  },
        { Id.UnknownName12_Legacy,  new Color32(228, 214, 218, 128) },
    };

        // RGB BI Render 2019.01.22 - provided by Klaes Simonsen
        private readonly static Dictionary<Id, Color> idToBI = new Dictionary<Id, Color>()
    {
        // Universal.
        { Id.White,                             new Color32(239, 239, 239, 255) },
        { Id.BrickYellow,                       new Color32(213, 193, 138, 255) },
        { Id.BrightRed,                         new Color32(200, 0, 0, 255)     },
        { Id.BrightBlue,                        new Color32(0, 95, 173, 255)    },
        { Id.BrightYellow,                      new Color32(243, 204, 0, 255)   },
        { Id.Black,                             new Color32(50, 50, 50, 255)    },
        { Id.DarkGreen,                         new Color32(54, 147, 83, 255)   },
        { Id.ReddishBrown,                      new Color32(86, 40, 17, 255)    },
        { Id.MediumStoneGrey,                   new Color32(155, 155, 155, 255) },
        { Id.DarkStoneGrey,                     new Color32(105, 105, 105, 255) },

        // Generic.
        { Id.Nougat,                            new Color32(202, 138, 97, 255)  },
        { Id.BrightGreen,                       new Color32(88, 171, 70, 255)   },
        { Id.Transparent,                       new Color32(234, 234, 234, 128) },
        { Id.TransparentRed,                    new Color32(221, 0, 16, 128)    },
        { Id.TransparentLightBlue,              new Color32(155, 205, 178, 128) },
        { Id.TransparentBlue,                   new Color32(0, 140, 197, 128)   },
        { Id.TransparentYellow,                 new Color32(255, 218, 16, 128)  },
        { Id.TransparentGreen,                  new Color32(12, 152, 34, 128)   },
        { Id.TransparentFluorescentGreen,       new Color32(221, 219, 0, 128)   },
        { Id.MediumBlue,                        new Color32(115, 150, 200, 255) },
        { Id.BrightOrange,                      new Color32(240, 129, 30, 255)  },
        { Id.TransparentBrown,                  new Color32(115, 104, 94, 128)  },
        { Id.BrightYellowGreen,                 new Color32(149, 186, 63, 255)  },
        { Id.EarthBlue,                         new Color32(30, 55, 95, 255)    },
        { Id.EarthGreen,                        new Color32(39, 75, 50, 255)    },
        { Id.NewDarkRed,                        new Color32(118, 35, 48, 255)   },
        { Id.TransparentBrightOrange,           new Color32(204, 140, 0, 128)   },
        { Id.BrightPurple,                      new Color32(231, 74, 182, 255)  },
        { Id.LightPurple,                       new Color32(243, 154, 220, 255) },
        { Id.MediumAzur,                        new Color32(110, 185, 215, 255) },
        { Id.MediumLavender,                    new Color32(160, 110, 185, 255) },

        // Special.
        { Id.DarkOrange,                        new Color32(153, 81, 48, 255)   },
        { Id.TransparentFluorescentRedOrange,   new Color32(255, 71, 0, 128)    },
        { Id.BrightBluishGreen,                 new Color32(31, 163, 158, 255)  },
        { Id.TransparentMediumViolet,           new Color32(211, 77, 143, 128)  },
        { Id.BrightReddishViolet,               new Color32(144, 35, 127, 255)  },
        { Id.TransparentBrightViolet,           new Color32(114, 101, 165, 128) },
        { Id.SandBlue,                          new Color32(112, 129, 154, 255) },
        { Id.SandYellow,                        new Color32(137, 125, 98, 255)  },
        { Id.SandGreen,                         new Color32(116, 154, 130, 255) },
        { Id.FlameYellowOrange,                 new Color32(250, 165, 40, 255)  },
        { Id.LightRoyalBlue,                    new Color32(140, 195, 232, 255) },
        { Id.CoolYellow,                        new Color32(255, 227, 113, 255) },
        { Id.MediumLilac,                       new Color32(75, 55, 138, 255)   },
        { Id.LightNougat,                       new Color32(251, 189, 155, 255) },
        { Id.WarmGold,                          new Color32(144, 85, 13, 255)   },
        { Id.DarkBrown,                         new Color32(53, 33, 0, 255)     },
        { Id.TransparentBrightGreen,            new Color32(175, 210, 70, 128)  },
        { Id.MediumNougat,                      new Color32(152, 107, 72, 255)  },
        { Id.SilverMetallic,                    new Color32(140, 132, 129, 255) },
        { Id.TitaniumMetallic,                  new Color32(72, 72, 72, 255)    },
        { Id.DarkAzur,                          new Color32(60, 150, 200, 255)  },
        { Id.Aqua,                              new Color32(211, 242, 234, 255) },
        { Id.Lavender,                          new Color32(205, 164, 222, 255) },
        { Id.SpringYellowishGreen,              new Color32(215, 230, 150, 255) },
        { Id.WhiteGlow,                         new Color32(245, 243, 215, 255) },
        { Id.OliveGreen,                        new Color32(121, 127, 55, 255)  },
        { Id.VibrantCoral,                      new Color32(255, 85, 125, 255)  },

        // Legacy.
        { Id.CopperMetallicInk_Legacy,          new Color32(116, 77, 59, 255)   },
        { Id.GoldMetallicInk_Legacy,            new Color32(185, 149, 59, 255)  },
        { Id.SilverMetallicInk_Legacy,          new Color32(140, 140, 140, 255) },
        { Id.TitaniumMetallicInk_Legacy,        new Color32(62, 60, 57, 255)    },
    };

        private readonly static Dictionary<Color, Id> colourGuideToId = new Dictionary<Color, Id>()
    {
        // Universal.
        { new Color32(244, 244, 244, 255), Id.White                             },
        { new Color32(204, 185, 141, 255), Id.BrickYellow                       },
        { new Color32(180, 0, 0, 255),     Id.BrightRed                         },
        { new Color32(30, 90, 168, 255),   Id.BrightBlue                        },
        { new Color32(250, 200, 10, 255),  Id.BrightYellow                      },
        { new Color32(0, 0, 0, 255),       Id.Black                             },
        { new Color32(0, 133, 43, 255),    Id.DarkGreen                         },
        { new Color32(95, 49, 9, 255),     Id.ReddishBrown                      },
        { new Color32(150, 150, 150, 255), Id.MediumStoneGrey                   },
        { new Color32(100, 100, 100, 255), Id.DarkStoneGrey                     },
        
        // Generic.
        { new Color32(187, 128, 90, 255),  Id.Nougat                            },
        { new Color32(88, 171, 65, 255),   Id.BrightGreen                       },
        { new Color32(238, 238, 238, 128), Id.Transparent                       },
        { new Color32(184, 0, 0, 128),     Id.TransparentRed                    },
        { new Color32(173, 221, 237, 128), Id.TransparentLightBlue              },
        { new Color32(0, 133, 184, 128),   Id.TransparentBlue                   },
        { new Color32(255, 230, 34, 128),  Id.TransparentYellow                 },
        { new Color32(115, 180, 100, 128), Id.TransparentGreen                  },
        { new Color32(250, 241, 91, 128),  Id.TransparentFluorescentGreen       },
        { new Color32(115, 150, 200, 255), Id.MediumBlue                        },
        { new Color32(214, 121, 35, 255),  Id.BrightOrange                      },
        { new Color32(187, 178, 158, 128), Id.TransparentBrown                  },
        { new Color32(165, 202, 24, 255),  Id.BrightYellowGreen                 },
        { new Color32(25, 25, 50, 255),    Id.EarthBlue                         },
        { new Color32(0, 69, 26, 255),     Id.EarthGreen                        },
        { new Color32(114, 0, 18, 255),    Id.NewDarkRed                        },
        { new Color32(225, 141, 10, 128),  Id.TransparentBrightOrange           },
        { new Color32(200, 80, 155, 255),  Id.BrightPurple                      },
        { new Color32(255, 158, 205, 255), Id.LightPurple                       },
        { new Color32(104, 195, 226, 255), Id.MediumAzur                        },
        { new Color32(154, 118, 174, 255), Id.MediumLavender                    },

        // Special.
        { new Color32(145, 80, 28, 255),   Id.DarkOrange                        },
        { new Color32(203, 78, 41, 128),   Id.TransparentFluorescentRedOrange   },
        { new Color32(0, 152, 148, 255),   Id.BrightBluishGreen                 },
        { new Color32(253, 142, 207, 128), Id.TransparentMediumViolet           },
        { new Color32(144, 31, 118, 255),  Id.BrightReddishViolet               },
        { new Color32(111, 122, 184, 128), Id.TransparentBrightViolet           },
        { new Color32(112, 129, 154, 255), Id.SandBlue                          },
        { new Color32(137, 125, 98, 255),  Id.SandYellow                        },
        { new Color32(112, 142, 124, 255), Id.SandGreen                         },
        { new Color32(252, 172, 0, 255),   Id.FlameYellowOrange                 },
        { new Color32(157, 195, 247, 255), Id.LightRoyalBlue                    },
        { new Color32(255, 236, 108, 255), Id.CoolYellow                        },
        { new Color32(68, 26, 145, 255),   Id.MediumLilac                       },
        { new Color32(225, 190, 161, 255), Id.LightNougat                       },
        { new Color32(175, 133, 34, 255),  Id.WarmGold                          },
        { new Color32(53, 33, 0, 255),     Id.DarkBrown                         },
        { new Color32(175, 210, 70, 128),  Id.TransparentBrightGreen            },
        { new Color32(170, 125, 85, 255),  Id.MediumNougat                      },
        { new Color32(140, 140, 140, 255), Id.SilverMetallic                    },
        { new Color32(73, 70, 68, 255),    Id.TitaniumMetallic                  },
        { new Color32(70, 155, 195, 255),  Id.DarkAzur                          },
        { new Color32(211, 242, 234, 255), Id.Aqua                              },
        { new Color32(205, 164, 222, 255), Id.Lavender                          },
        { new Color32(226, 249, 154, 255), Id.SpringYellowishGreen              },
        { new Color32(245, 243, 215, 255), Id.WhiteGlow                         },
        { new Color32(139, 132, 79, 255),  Id.OliveGreen                        },
        { new Color32(240, 109, 120, 255), Id.VibrantCoral                      },

        // Legacy.
        { new Color32(138, 146, 141, 255), Id.Grey_Legacy                               },
        { new Color32(255, 214, 127, 255), Id.LightYellow_Legacy                        },
        { new Color32(173, 217, 168, 255), Id.LightGreen_Legacy                         },
        { new Color32(246, 169, 187, 255), Id.LightReddishViolet_Legacy                 },
        { new Color32(171, 217, 255, 255), Id.PastelBlue_Legacy                         },
        { new Color32(216, 109, 44, 255),  Id.LightOrangeBrown_Legacy                   },
        { new Color32(233, 233, 233, 128), Id.Nature_Legacy                             },
        { new Color32(208, 80, 152, 255),  Id.MediumReddishViolet_Legacy                },
        { new Color32(84, 51, 36, 255),    Id.EarthOrange_Legacy                        },
        { new Color32(84, 89, 85, 255),    Id.DarkGrey_Legacy                           },
        { new Color32(127, 196, 117, 255), Id.MediumGreen_Legacy                        },
        { new Color32(253, 195, 131, 255), Id.LightYellowishOrange_Legacy               },
        { new Color32(175, 190, 214, 255), Id.LightBluishViolet_Legacy                  },
        { new Color32(151, 203, 217, 255), Id.LightBlue_Legacy                          },
        { new Color32(229, 223, 211, 255), Id.PhosphorescentWhite_Legacy                },
        { new Color32(249, 183, 165, 255), Id.LightRed_Legacy                           },
        { new Color32(240, 109, 97, 255),  Id.MediumRed_Legacy                          },
        { new Color32(188, 180, 165, 255), Id.LightGrey_Legacy                          },
        { new Color32(103, 31, 161, 255),  Id.BrightViolet_Legacy                       },
        { new Color32(245, 134, 36, 255),  Id.BrightYellowishOrange_Legacy              },
        { new Color32(86, 71, 47, 255),    Id.EarthYellow_Legacy                        },
        { new Color32(0, 20, 20, 255),     Id.PcBlackIr_Legacy                          },
        { new Color32(38, 70, 154, 255),   Id.BrightBluishViolet_Legacy                 },
        { new Color32(72, 97, 172, 255),   Id.MediumBluishViolet_Legacy                 },
        { new Color32(183, 212, 37, 255),  Id.MediumYellowishGreen_Legacy               },
        { new Color32(0, 170, 164, 255),   Id.MediumBluishGreen_Legacy                  },
        { new Color32(156, 214, 204, 255), Id.LightBluishGreen_Legacy                   },
        { new Color32(238, 84, 52, 255),   Id.BrightReddishOrange_Legacy                },
        { new Color32(222, 234, 146, 255), Id.LightYellowishGreen_Legacy                },
        { new Color32(248, 154, 57, 255),  Id.MediumYellowishOrange_Legacy              },
        { new Color32(249, 167, 119, 255), Id.LightOrange_Legacy                        },
        { new Color32(222, 172, 102, 255), Id.Gold_Legacy                               },
        { new Color32(173, 97, 64, 255),   Id.DarkNougat_Legacy                         },
        { new Color32(160, 160, 160, 255), Id.Silver_Legacy                             },
        { new Color32(239, 88, 40, 255),   Id.NeonOrange_Legacy                         },
        { new Color32(205, 221, 52, 255),  Id.NeonGreen_Legacy                          },
        { new Color32(117, 101, 125, 255), Id.SandViolet_Legacy                         },
        { new Color32(244, 129, 71, 255),  Id.MediumOrange_Legacy                       },
        { new Color32(118, 77, 59, 255),   Id.Copper_Legacy                             },
        { new Color32(149, 189, 221, 128), Id.TransparentFlourescentBlue_Legacy         },
        { new Color32(91, 117, 144, 255),  Id.MetallicSandBlue_Legacy                   },
        { new Color32(129, 117, 144, 255), Id.MetallicSandViolet_Legacy                 },
        { new Color32(131, 114, 79, 255),  Id.MetallicSandYellow_Legacy                 },
        { new Color32(72, 77, 72, 255),    Id.MetallicDarkGrey_Legacy                   },
        { new Color32(10, 19, 39, 255),    Id.MetallicBlack_Legacy                      },
        { new Color32(152, 155, 153, 255), Id.MetallicLightGrey_Legacy                  },
        { new Color32(136, 96, 94, 255),   Id.SandRed_Legacy                            },
        { new Color32(255, 246, 92, 128),  Id.TransparentFlourescentYellow_Legacy       },
        { new Color32(241, 142, 187, 128), Id.TransparentFlourescentRed_Legacy          },
        { new Color32(96, 86, 76, 255),    Id.GunMetallic_Legacy                        },
        { new Color32(148, 81, 72, 255),   Id.RedFlipFlop_Legacy                        },
        { new Color32(171, 103, 58, 255),  Id.YellowFlipFlop_Legacy                     },
        { new Color32(115, 114, 113, 255), Id.SilverFlipFlop_Legacy                     },
        { new Color32(221, 152, 46, 255),  Id.Curry_Legacy                              },
        { new Color32(246, 242, 223, 255), Id.MetallicWhite_Legacy                      },
        { new Color32(214, 0, 38, 255),    Id.MetallicBrightRed_Legacy                  },
        { new Color32(0, 89, 163, 255),    Id.MetallicBrightBlue_Legacy                 },
        { new Color32(0, 142, 60, 255),    Id.MetallicDarkGreen_Legacy                  },
        { new Color32(87, 57, 44, 255),    Id.MetallicEarthOrange_Legacy                },
        { new Color32(0, 158, 206, 255),   Id.TinyBlue_Legacy                           },
        { new Color32(172, 130, 71, 255),  Id.ReddishGold_Legacy                        },
        { new Color32(255, 207, 11, 255),  Id.FireYellow_Legacy                         },
        { new Color32(236, 68, 29, 255),   Id.FlameReddishOrange_Legacy                 },
        { new Color32(28, 88, 167, 255),   Id.RoyalBlue_Legacy                          },
        { new Color32(14, 62, 154, 255),   Id.DarkRoyalBlue_Legacy                      },
        { new Color32(49, 43, 135, 255),   Id.BrightLilac_Legacy                        },
        { new Color32(138, 18, 168, 255),  Id.BrightReddishLilac_Legacy                 },
        { new Color32(106, 121, 68, 255),  Id.MetallicLemon_Legacy                      },
        { new Color32(200, 200, 200, 255), Id.LightStoneGrey_Legacy                     },
        { new Color32(164, 118, 36, 255),  Id.DarkCurry_Legacy                          },
        { new Color32(70, 138, 95, 255),   Id.FadedGreen_Legacy                         },
        { new Color32(63, 182, 169, 255),  Id.Turquoise_Legacy                          },
        { new Color32(71, 111, 182, 255),  Id.MediumRoyalBlue_Legacy                    },
        { new Color32(135, 43, 23, 255),   Id.Rust_Legacy                               },
        { new Color32(123, 93, 65, 255),   Id.Brown_Legacy                              },
        { new Color32(142, 85, 151, 255),  Id.ReddishLilac_Legacy                       },
        { new Color32(86, 78, 157, 255),   Id.Lilac_Legacy                              },
        { new Color32(145, 149, 202, 255), Id.LightLilac_Legacy                         },
        { new Color32(241, 120, 128, 255), Id.LightPink_Legacy                          },
        { new Color32(243, 201, 136, 255), Id.LightBrickYellow_Legacy                   },
        { new Color32(250, 169, 100, 255), Id.WarmYellowishOrange_Legacy                },
        { new Color32(201, 231, 136, 128), Id.TransparentBrightYellowishGreen_Legacy    },
        { new Color32(172, 212, 222, 128), Id.TransparentLightBluishGreen_Legacy        },
        { new Color32(236, 163, 201, 128), Id.TransparentBrightPurple_Legacy            },
        { new Color32(252, 183, 109, 128), Id.TransparentFlameYellowishOrange_Legacy    },
        { new Color32(119, 201, 216, 255), Id.DoveBlue_Legacy                           },
        { new Color32(96, 186, 118, 255),  Id.LightFadedGreen_Legacy                    },
        { new Color32(251, 232, 144, 128), Id.TransparentFireYellow_Legacy              },
        { new Color32(141, 115, 179, 128), Id.TransparentBrightReddishLilac_Legacy      },
        { new Color32(62, 149, 182, 255),  Id.MediumBlue_Legacy                         },
        { new Color32(224, 208, 229, 128), Id.TransparentReddishLilac_Legacy            },
        { new Color32(213, 220, 138, 128), Id.PhosphorescentGreen_Legacy                },
        { new Color32(173, 173, 173, 255), Id.CoolSilver_Legacy                         },
        { new Color32(118, 118, 118, 255), Id.CoolSilverDrumLacq_Legacy                 },
        { new Color32(206, 206, 206, 255), Id.MetalizedSilver_Legacy                    },
        { new Color32(223, 193, 118, 255), Id.MetalizedGold_Legacy                      },
        { new Color32(116, 77, 59, 255),   Id.CopperMetallicInk_Legacy                  },
        { new Color32(185, 149, 59, 255),  Id.GoldMetallicInk_Legacy                    },
        //{ new Color32(140, 140, 140, 255), Id.SilverMetallicInk_Legacy                  }, // Same key as Id.SilverMetallic.
        { new Color32(62, 60, 57, 255),    Id.TitaniumMetallicInk_Legacy                },

        // Legacy w/ Unknown Name
        { new Color32(242, 112, 94, 255),  Id.UnknownName00_Legacy },
        { new Color32(255, 133, 0, 255),   Id.UnknownName01_Legacy },
        { new Color32(140, 0, 255, 255),   Id.UnknownName02_Legacy },
        { new Color32(255, 255, 189, 128), Id.UnknownName03_Legacy },
        { new Color32(255, 128, 20, 255),  Id.UnknownName04_Legacy },
        { new Color32(120, 252, 120, 255), Id.UnknownName05_Legacy },
        { new Color32(255, 242, 48, 255),  Id.UnknownName06_Legacy },
        { new Color32(255, 135, 156, 255), Id.UnknownName07_Legacy },
        { new Color32(255, 148, 148, 255), Id.UnknownName08_Legacy },
        { new Color32(207, 138, 71, 255),  Id.UnknownName09_Legacy },
        { new Color32(254, 203, 152, 255), Id.UnknownName10_Legacy },
        { new Color32(85, 165, 175, 128),  Id.UnknownName11_Legacy },
        { new Color32(228, 214, 218, 128), Id.UnknownName12_Legacy },
    };

        private static Dictionary<Color, Id> biToId = new Dictionary<Color, Id>()
    {
        // Universal.
        { new Color32(239, 239, 239, 255), Id.White                             },
        { new Color32(213, 193, 138, 255), Id.BrickYellow                       },
        { new Color32(200, 0, 0, 255),     Id.BrightRed                         },
        { new Color32(0, 95, 173, 255),    Id.BrightBlue                        },
        { new Color32(243, 204, 0, 255),   Id.BrightYellow                      },
        { new Color32(50, 50, 50, 255),    Id.Black                             },
        { new Color32(54, 147, 83, 255),   Id.DarkGreen                         },
        { new Color32(86, 40, 17, 255),    Id.ReddishBrown                      },
        { new Color32(155, 155, 155, 255), Id.MediumStoneGrey                   },
        { new Color32(105, 105, 105, 255), Id.DarkStoneGrey                     },

        // Generic.
        { new Color32(202, 138, 97, 255),  Id.Nougat                            },
        { new Color32(88, 171, 70, 255),   Id.BrightGreen                       },
        { new Color32(234, 234, 234, 128), Id.Transparent                       },
        { new Color32(221, 0, 16, 128),    Id.TransparentRed                    },
        { new Color32(155, 205, 178, 128), Id.TransparentLightBlue              },
        { new Color32(0, 140, 197, 128),   Id.TransparentBlue                   },
        { new Color32(255, 218, 16, 128),  Id.TransparentYellow                 },
        { new Color32(12, 152, 34, 128),   Id.TransparentGreen                  },
        { new Color32(221, 219, 0, 128),   Id.TransparentFluorescentGreen       },
        { new Color32(115, 150, 200, 255), Id.MediumBlue                        },
        { new Color32(240, 129, 30, 255),  Id.BrightOrange                      },
        { new Color32(115, 104, 94, 128),  Id.TransparentBrown                  },
        { new Color32(149, 186, 63, 255),  Id.BrightYellowGreen                 },
        { new Color32(30, 55, 95, 255),    Id.EarthBlue                         },
        { new Color32(39, 75, 50, 255),    Id.EarthGreen                        },
        { new Color32(118, 35, 48, 255),   Id.NewDarkRed                        },
        { new Color32(204, 140, 0, 128),   Id.TransparentBrightOrange           },
        { new Color32(231, 74, 182, 255),  Id.BrightPurple                      },
        { new Color32(243, 154, 220, 255), Id.LightPurple                       },
        { new Color32(110, 185, 215, 255), Id.MediumAzur                        },
        { new Color32(160, 110, 185, 255), Id.MediumLavender                    },

        // Special.
        { new Color32(153, 81, 48, 255),   Id.DarkOrange                        },
        { new Color32(255, 71, 0, 128),    Id.TransparentFluorescentRedOrange   },
        { new Color32(31, 163, 158, 255),  Id.BrightBluishGreen                 },
        { new Color32(211, 77, 143, 128),  Id.TransparentMediumViolet           },
        { new Color32(144, 35, 127, 255),  Id.BrightReddishViolet               },
        { new Color32(114, 101, 165, 128), Id.TransparentBrightViolet           },
        { new Color32(112, 129, 154, 255), Id.SandBlue                          },
        { new Color32(137, 125, 98, 255),  Id.SandYellow                        },
        { new Color32(116, 154, 130, 255), Id.SandGreen                         },
        { new Color32(250, 165, 40, 255),  Id.FlameYellowOrange                 },
        { new Color32(140, 195, 232, 255), Id.LightRoyalBlue                    },
        { new Color32(255, 227, 113, 255), Id.CoolYellow                        },
        { new Color32(75, 55, 138, 255),   Id.MediumLilac                       },
        { new Color32(251, 189, 155, 255), Id.LightNougat                       },
        { new Color32(144, 85, 13, 255),   Id.WarmGold                          },
        { new Color32(53, 33, 0, 255),     Id.DarkBrown                         },
        { new Color32(175, 210, 70, 128),  Id.TransparentBrightGreen            },
        { new Color32(152, 107, 72, 255),  Id.MediumNougat                      },
        { new Color32(140, 132, 129, 255), Id.SilverMetallic                    },
        { new Color32(72, 72, 72, 255),    Id.TitaniumMetallic                  },
        { new Color32(60, 150, 200, 255),  Id.DarkAzur                          },
        { new Color32(211, 242, 234, 255), Id.Aqua                              },
        { new Color32(205, 164, 222, 255), Id.Lavender                          },
        { new Color32(215, 230, 150, 255), Id.SpringYellowishGreen              },
        { new Color32(245, 243, 215, 255), Id.WhiteGlow                         },
        { new Color32(121, 127, 55, 255),  Id.OliveGreen                        },
        { new Color32(255, 85, 125, 255),  Id.VibrantCoral                      },

        // Legacy.
        { new Color32(116, 77, 59, 255),   Id.CopperMetallicInk_Legacy          },
        { new Color32(185, 149, 59, 255),  Id.GoldMetallicInk_Legacy            },
        { new Color32(140, 140, 140, 255), Id.SilverMetallicInk_Legacy          },
        { new Color32(62, 60, 57, 255),    Id.TitaniumMetallicInk_Legacy        },
    };

        private readonly static HashSet<Id> legacy = new HashSet<Id>()
    {
        Id.Grey_Legacy,
        Id.LightYellow_Legacy,
        Id.LightGreen_Legacy,
        Id.LightReddishViolet_Legacy,
        Id.PastelBlue_Legacy,
        Id.LightOrangeBrown_Legacy,
        Id.Nature_Legacy,
        Id.MediumReddishViolet_Legacy,
        Id.EarthOrange_Legacy,
        Id.DarkGrey_Legacy,
        Id.MediumGreen_Legacy,
        Id.LightYellowishOrange_Legacy,
        Id.LightBluishViolet_Legacy,
        Id.LightBlue_Legacy,
        Id.PhosphorescentWhite_Legacy,
        Id.LightRed_Legacy,
        Id.MediumRed_Legacy,
        Id.LightGrey_Legacy,
        Id.BrightViolet_Legacy,
        Id.BrightYellowishOrange_Legacy,
        Id.EarthYellow_Legacy,
        Id.PcBlackIr_Legacy,
        Id.BrightBluishViolet_Legacy,
        Id.MediumBluishViolet_Legacy,
        Id.MediumYellowishGreen_Legacy,
        Id.MediumBluishGreen_Legacy,
        Id.LightBluishGreen_Legacy,
        Id.BrightReddishOrange_Legacy,
        Id.LightYellowishGreen_Legacy,
        Id.MediumYellowishOrange_Legacy,
        Id.LightOrange_Legacy,
        Id.Gold_Legacy,
        Id.DarkNougat_Legacy,
        Id.Silver_Legacy,
        Id.NeonOrange_Legacy,
        Id.NeonGreen_Legacy,
        Id.SandViolet_Legacy,
        Id.MediumOrange_Legacy,
        Id.Copper_Legacy,
        Id.TransparentFlourescentBlue_Legacy,
        Id.MetallicSandBlue_Legacy,
        Id.MetallicSandViolet_Legacy,
        Id.MetallicSandYellow_Legacy,
        Id.MetallicDarkGrey_Legacy,
        Id.MetallicBlack_Legacy,
        Id.MetallicLightGrey_Legacy,
        Id.SandRed_Legacy,
        Id.TransparentFlourescentYellow_Legacy,
        Id.TransparentFlourescentRed_Legacy,
        Id.GunMetallic_Legacy,
        Id.RedFlipFlop_Legacy,
        Id.YellowFlipFlop_Legacy,
        Id.SilverFlipFlop_Legacy,
        Id.Curry_Legacy,
        Id.MetallicWhite_Legacy,
        Id.MetallicBrightRed_Legacy,
        Id.MetallicBrightBlue_Legacy,
        Id.MetallicDarkGreen_Legacy,
        Id.MetallicEarthOrange_Legacy,
        Id.TinyBlue_Legacy,
        Id.ReddishGold_Legacy,
        Id.FireYellow_Legacy,
        Id.FlameReddishOrange_Legacy,
        Id.RoyalBlue_Legacy,
        Id.DarkRoyalBlue_Legacy,
        Id.BrightLilac_Legacy,
        Id.BrightReddishLilac_Legacy,
        Id.MetallicLemon_Legacy,
        Id.LightStoneGrey_Legacy,
        Id.DarkCurry_Legacy,
        Id.FadedGreen_Legacy,
        Id.Turquoise_Legacy,
        Id.MediumRoyalBlue_Legacy,
        Id.Rust_Legacy,
        Id.Brown_Legacy,
        Id.ReddishLilac_Legacy,
        Id.Lilac_Legacy,
        Id.LightLilac_Legacy,
        Id.LightPink_Legacy,
        Id.LightBrickYellow_Legacy,
        Id.WarmYellowishOrange_Legacy,
        Id.TransparentBrightYellowishGreen_Legacy,
        Id.TransparentLightBluishGreen_Legacy,
        Id.TransparentBrightPurple_Legacy,
        Id.TransparentFlameYellowishOrange_Legacy,
        Id.DoveBlue_Legacy,
        Id.LightFadedGreen_Legacy,
        Id.TransparentFireYellow_Legacy,
        Id.TransparentBrightReddishLilac_Legacy,
        Id.MediumBlue_Legacy,
        Id.TransparentReddishLilac_Legacy,
        Id.PhosphorescentGreen_Legacy,
        Id.CoolSilver_Legacy,
        Id.CoolSilverDrumLacq_Legacy,
        Id.MetalizedSilver_Legacy,
        Id.MetalizedGold_Legacy,
        Id.CopperMetallicInk_Legacy,
        Id.GoldMetallicInk_Legacy,
        Id.SilverMetallicInk_Legacy,
        Id.TitaniumMetallicInk_Legacy,
        Id.UnknownName00_Legacy,
        Id.UnknownName01_Legacy,
        Id.UnknownName02_Legacy,
        Id.UnknownName03_Legacy,
        Id.UnknownName04_Legacy,
        Id.UnknownName05_Legacy,
        Id.UnknownName06_Legacy,
        Id.UnknownName07_Legacy,
        Id.UnknownName08_Legacy,
        Id.UnknownName09_Legacy,
        Id.UnknownName10_Legacy,
        Id.UnknownName11_Legacy,
        Id.UnknownName12_Legacy,
    };

// FIXME Remove when colour palette experiments are over.
#if UNITY_EDITOR
        private const string useBIPrefsKey = "com.unity.lego.materials.useBI";
        private const string useBIMenuPath = "LEGO Tools/Dev/Use BI Materials";

        //[MenuItem(useBIMenuPath)]
        private static void ToggleBI()
        {
            useBI = !useBI;
            EditorPrefs.SetBool(useBIPrefsKey, useBI);
        }

        //[MenuItem(useBIMenuPath, true)]
        private static bool ValidateToggeBI()
        {
            Menu.SetChecked(useBIMenuPath, useBI);
            return true;
        }

        private static bool useBI;

        static MouldingColour()
        {
            useBI = EditorPrefs.GetBool(useBIPrefsKey, false);
        }

        public static bool GetBI()
        {
            return useBI;
        }
#endif
        
        public static Color GetColour(Id id)
        {
// FIXME Remove when colour palette experiments are over.
#if UNITY_EDITOR
            if (useBI)
            {
                if (idToBI.ContainsKey(id))
                {
                    return idToBI[id];
                }
            }
#endif
            if (idToColourGuide.ContainsKey(id))
            {
                return idToColourGuide[id];
            }
            else
            {
                Debug.LogError("Moulding color id " + id + " is missing a colour");
                return Color.black;
            }
        }

        public static Color GetColour(string id)
        {
            try
            {
                return GetColour((Id)Enum.Parse(typeof(Id), id));
            }
            catch
            {
                Debug.LogErrorFormat("Invalid moulding colour id {0}", id);
                return Color.black;
            }
        }

        public static Color GetColour(int id)
        {
            return GetColour(id.ToString());
        }

        public static Id GetId(Color colour)
        {
// FIXME Remove when colour palette experiments are over.
#if UNITY_EDITOR
            if (useBI)
            {
                if (biToId.ContainsKey(colour))
                {
                    return biToId[colour];
                }
            }
#endif
            if (colourGuideToId.ContainsKey(colour))
            {
                return colourGuideToId[colour];
            }
            else
            {
                Debug.LogErrorFormat("Invalid moulding colour value {0}", colour);
                return Id.Black;
            }
        }

        public static bool IsLegacy(Id id)
        {
            return legacy.Contains(id);
        }

        public static bool IsLegacy(string id)
        {
            try
            {
                return IsLegacy((Id)Enum.Parse(typeof(Id), id));
            }
            catch
            {
                Debug.LogErrorFormat("Invalid moulding colour id {0}", id);
                return true;
            }
        }

        public static bool IsLegacy(int id)
        {
            return IsLegacy(id.ToString());
        }

        public static bool IsTransparent(Id id)
        {
// FIXME Remove when colour palette experiments are over.
#if UNITY_EDITOR
            if (useBI)
            {
                if (idToBI.ContainsKey(id))
                {
                    return idToBI[id].a < 1.0f;
                }
            }
#endif
            if (idToColourGuide.ContainsKey(id))
            {
                return idToColourGuide[id].a < 1.0f;
            }
            else
            {
                Debug.LogError("Moulding color id " + id + " is missing a colour");
                return false;
            }

        }

        public static bool IsTransparent(string id)
        {
            try
            {
                return IsTransparent((Id)Enum.Parse(typeof(Id), id));
            }
            catch
            {
                Debug.LogErrorFormat("Invalid moulding colour id {0}", id);
                return true;
            }
        }

        public static bool IsTransparent(int id)
        {
            return IsTransparent(id.ToString());
        }

        public static bool IsAnyTransparent(List<int> ids)
        {
            foreach(var id in ids)
            {
                if (IsTransparent(id))
                {
                    return true;
                }
            }

            return false;
        }

    }

}