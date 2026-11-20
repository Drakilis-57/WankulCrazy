using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WankulCrazyPlugin.cards;

namespace WankulCrazyPlugin.patch
{
    internal class ExpansionScreen
    {
        public static bool inited = false;
        public static int currentExpensionIndex = 0;
        public static void OpenExpansionScreen(ECardExpansionType initCardExpansion)
        {
            if (inited)
            {
                for (int i = 0; i < CardExpansionSelectScreen.Instance.m_BtnHighlightList.Count; i++)
                {
                    CardExpansionSelectScreen.Instance.m_BtnHighlightList[i].SetActive(false);
                }
                CardExpansionSelectScreen.Instance.m_BtnHighlightList[currentExpensionIndex].SetActive(true);
                return;
            }

            inited = true;

            Transform AnimGroupTranform = Plugin.GetByPathIn("Canvas", "CardExpansionSelectScreen/Screen_Grp/AnimGrp");
            RectTransform AnimGroupRectTransform = AnimGroupTranform.GetComponent<RectTransform>();
            AnimGroupRectTransform.sizeDelta = new Vector2(AnimGroupRectTransform.sizeDelta.x, 1800);

            Transform BGTranform = Plugin.GetByPathIn("Canvas", "CardExpansionSelectScreen/Screen_Grp/AnimGrp/BG");
            RectTransform BGRectTransform = BGTranform.GetComponent<RectTransform>();
            BGRectTransform.sizeDelta = new Vector2(BGRectTransform.sizeDelta.x, 1000);

            Transform MaskTranform = Plugin.GetByPathIn("Canvas", "CardExpansionSelectScreen/Screen_Grp/AnimGrp/Mask");
            RectTransform MaskRectTransform = MaskTranform.GetComponent<RectTransform>();
            MaskRectTransform.sizeDelta = new Vector2(MaskRectTransform.sizeDelta.x, 600);


            Transform tetramonButton = Plugin.FindChildByPath(CSingleton<CardExpansionSelectScreen>.Instance.m_ScreenGrp.transform, "AnimGrp/Mask/UIGroup/Tetramon_Button");
            tetramonButton.GetComponentInChildren<TextMeshProUGUI>().text = "Origins";
            tetramonButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                0,
                220
            );

            Transform destinyButton = Plugin.FindChildByPath(CSingleton<CardExpansionSelectScreen>.Instance.m_ScreenGrp.transform, "AnimGrp/Mask/UIGroup/Destiny_Button");
            destinyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Campus";

            Transform ghostButton = Plugin.FindChildByPath(CSingleton<CardExpansionSelectScreen>.Instance.m_ScreenGrp.transform, "AnimGrp/Mask/UIGroup/Ghost_Button");
            ghostButton.GetComponentInChildren<TextMeshProUGUI>().text = "Battle";

            float verticalSpacing = destinyButton.gameObject.GetComponent<RectTransform>().anchoredPosition.y - ghostButton.gameObject.GetComponent<RectTransform>().anchoredPosition.y - 15;


            destinyButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                tetramonButton.GetComponent<RectTransform>().anchoredPosition.x,
                tetramonButton.GetComponent<RectTransform>().anchoredPosition.y - verticalSpacing
            );

            ghostButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                destinyButton.GetComponent<RectTransform>().anchoredPosition.x,
                destinyButton.GetComponent<RectTransform>().anchoredPosition.y - verticalSpacing
            );


            Vector2 previousPos = ghostButton.GetComponent<RectTransform>().anchoredPosition;
            Season[] extraSeasons = { Season.S04, Season.S05, Season.HS };

            foreach (Season season in extraSeasons)
            {
                GameObject seasonObj = GameObject.Instantiate(destinyButton.gameObject);
                seasonObj.name = $"{season}_Button";
                seasonObj.transform.SetParent(tetramonButton.parent);
                seasonObj.transform.localScale = tetramonButton.localScale;
                seasonObj.transform.localPosition = tetramonButton.localPosition;
                seasonObj.transform.localRotation = tetramonButton.localRotation;

                Vector2 nextPos = new Vector2(previousPos.x, previousPos.y - verticalSpacing);
                seasonObj.GetComponent<RectTransform>().anchoredPosition = nextPos;
                previousPos = nextPos;

                string label = SeasonsContainer.Seasons.TryGetValue(season, out string seasonName) ? seasonName : season.ToString();
                seasonObj.GetComponentInChildren<TextMeshProUGUI>().text = label;

                Button seasonButton = Plugin.FindChildByPath(seasonObj.transform, "AnimGrp/BGBarGrp/BtnRaycast").GetComponent<Button>();
                GameObject highlight = Plugin.FindChildByPath(seasonObj.transform, "AnimGrp/BGHighlight").gameObject;
                CardExpansionSelectScreen.Instance.m_BtnHighlightList.Add(highlight);

                Season capturedSeason = season;
                seasonButton.onClick.AddListener(() =>
                {
                    highlight.SetActive(true);
                    Plugin.SetPProperty(CardExpansionSelectScreen.Instance, "m_CurrentIndex", (int)capturedSeason);
                    currentExpensionIndex = (int)capturedSeason;
                });
            }
        }

        public static void OnExpansionPressButton(int index)
        {
            currentExpensionIndex = index;
        }

    }
}
