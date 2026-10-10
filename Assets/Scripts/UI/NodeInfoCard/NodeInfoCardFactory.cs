using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HorizonFaser.UI
{
    public static class NodeInfoCardFactory
    {
        public static GameObject CreatePermitPopup(System.Action<string> onOpenPdf, System.Action onClose, out GameObject openPdfBtnObj, out TextMeshProUGUI openPdfBtnText)
        {
            GameObject canvasObj = new GameObject("Permit_Popup");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            try 
            { 
                var raycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
                if (raycasterType != null) canvasObj.AddComponent(raycasterType);
            } 
            catch { }

            if (Camera.main != null) canvas.worldCamera = Camera.main;

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(400, 620);
            canvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            GameObject bgObj = new GameObject("PermitImage");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RawImage bgImage = bgObj.AddComponent<RawImage>();
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.95f);
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.5f, 1); bgRT.anchorMax = new Vector2(0.5f, 1);
            bgRT.pivot = new Vector2(0.5f, 1);
            bgRT.sizeDelta = new Vector2(400, 560);
            bgRT.anchoredPosition = Vector2.zero;

            GameObject loadingObj = new GameObject("LoadingText");
            loadingObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI loadingText = loadingObj.AddComponent<TextMeshProUGUI>();
            loadingText.text = "Cargando documento...\nPor favor espera";
            loadingText.fontSize = 22;
            loadingText.fontStyle = FontStyles.Bold;
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            loadingText.raycastTarget = false;
            RectTransform loadingRT = loadingObj.GetComponent<RectTransform>();
            loadingRT.anchorMin = new Vector2(0.5f, 0.5f); loadingRT.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRT.pivot = new Vector2(0.5f, 0.5f);
            loadingRT.anchoredPosition = new Vector2(0, 50); 
            loadingRT.sizeDelta = new Vector2(350, 100);

            GameObject openBtnObj = new GameObject("OpenPdfButton");
            openBtnObj.transform.SetParent(canvasObj.transform, false);
            Image openBtnImg = openBtnObj.AddComponent<Image>();
            openBtnImg.sprite = UISpriteFactory.GetPillSprite();
            openBtnImg.type = Image.Type.Sliced;
            openBtnImg.color = new Color(0f, 0.48f, 1f, 1f);
            Button openBtn = openBtnObj.AddComponent<Button>();
            openBtnObj.SetActive(false);
            
            RectTransform openBtnRT = openBtnObj.GetComponent<RectTransform>();
            openBtnRT.anchorMin = new Vector2(0.5f, 0); openBtnRT.anchorMax = new Vector2(0.5f, 0);
            openBtnRT.pivot = new Vector2(0.5f, 0);
            openBtnRT.anchoredPosition = new Vector2(0, 60); 
            openBtnRT.sizeDelta = new Vector2(150, 40);

            GameObject openBtnTextObj = new GameObject("OpenBtnText");
            openBtnTextObj.transform.SetParent(openBtnObj.transform, false);
            TextMeshProUGUI openBtnTextLocal = openBtnTextObj.AddComponent<TextMeshProUGUI>();
            openBtnTextLocal.text = "VER PDF";
            openBtnTextLocal.fontSize = 20;
            openBtnTextLocal.fontStyle = FontStyles.Bold;
            openBtnTextLocal.alignment = TextAlignmentOptions.Center;
            openBtnTextLocal.color = Color.white;
            RectTransform openBtnTextRT = openBtnTextObj.GetComponent<RectTransform>();
            openBtnTextRT.anchorMin = Vector2.zero; openBtnTextRT.anchorMax = Vector2.one;
            openBtnTextRT.sizeDelta = Vector2.zero;
            
            openBtn.onClick.AddListener(() => {
                onOpenPdf?.Invoke("VER PDF");
            });

            openPdfBtnObj = openBtnObj;
            openPdfBtnText = openBtnTextLocal;

            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(canvasObj.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = UISpriteFactory.GetPillSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f); 
            Button btn = btnObj.AddComponent<Button>();
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0); btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, 10); 
            btnRT.sizeDelta = new Vector2(150, 40);

            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "CERRAR";
            btnText.fontSize = 20;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            RectTransform btnTextRT = btnTextObj.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
            btnTextRT.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => {
                onClose?.Invoke();
                canvasObj.SetActive(false);
            });

            UIXRUtils.SetLayerRecursively(canvasObj, 5); 
            UIXRUtils.MakeGrabbable(canvasObj, canvasRT.sizeDelta);
            canvasObj.SetActive(false);
            return canvasObj;
        }

        public static GameObject CreateDetailsPopup(out RawImage popupImage, System.Action onClose)
        {
            GameObject canvasObj = new GameObject("Details_Popup");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            try 
            { 
                var raycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
                if (raycasterType != null) canvasObj.AddComponent(raycasterType);
            } 
            catch { }

            if (Camera.main != null) canvas.worldCamera = Camera.main;

            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(800, 450); 
            canvasObj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = UISpriteFactory.GetRoundedSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.98f); 
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            GameObject titleObj = new GameObject("PopupTitleText");
            titleObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "TITULO";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -20);
            titleRT.sizeDelta = new Vector2(-40, 40);

            GameObject imgObj = new GameObject("PopupImage");
            imgObj.transform.SetParent(bgObj.transform, false);
            popupImage = imgObj.AddComponent<RawImage>();
            popupImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform imgRT = imgObj.GetComponent<RectTransform>();
            imgRT.anchorMin = new Vector2(0, 0); imgRT.anchorMax = new Vector2(0, 1);
            imgRT.pivot = new Vector2(0, 0.5f);
            imgRT.anchoredPosition = new Vector2(30, -10);
            imgRT.sizeDelta = new Vector2(300, -120);

            GameObject detailsObj = new GameObject("PopupDetailsText");
            detailsObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI details = detailsObj.AddComponent<TextMeshProUGUI>();
            details.text = "Cargando datos...";
            details.fontSize = 28;
            details.color = Color.white;
            details.alignment = TextAlignmentOptions.TopLeft;
            details.textWrappingMode = TextWrappingModes.Normal;
            details.overflowMode = TextOverflowModes.Overflow;
            RectTransform detailsRT = detailsObj.GetComponent<RectTransform>();
            detailsRT.anchorMin = new Vector2(0, 0); detailsRT.anchorMax = new Vector2(1, 1);
            detailsRT.pivot = new Vector2(0.5f, 0.5f);
            detailsRT.anchoredPosition = new Vector2(180, -30); 
            detailsRT.sizeDelta = new Vector2(-380, -160);

            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(bgObj.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = UISpriteFactory.GetPillSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button btn = btnObj.AddComponent<Button>();
            RectTransform btnRT = btnObj.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0); btnRT.anchorMax = new Vector2(0.5f, 0);
            btnRT.pivot = new Vector2(0.5f, 0);
            btnRT.anchoredPosition = new Vector2(0, 30);
            btnRT.sizeDelta = new Vector2(200, 50);

            GameObject btnTextObj = new GameObject("BtnText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "CERRAR";
            btnText.fontSize = 20;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;
            RectTransform btnTextRT = btnTextObj.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
            btnTextRT.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(() => {
                onClose?.Invoke();
                canvasObj.SetActive(false);
            });

            UIXRUtils.SetLayerRecursively(canvasObj, 5); 
            UIXRUtils.MakeGrabbable(canvasObj, canvasRT.sizeDelta);
            canvasObj.SetActive(false);
            return canvasObj;
        }

        public static NodeInfoCardUI CreateGeneratedCard()
        {
            GameObject canvasObj = new GameObject("Premium_NodeInfoCard");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            try 
            {
                var raycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
                if (raycasterType != null) canvasObj.AddComponent(raycasterType);
            } 
            catch { }

            if (Camera.main != null)
            {
                canvas.worldCamera = Camera.main;
            }
            
            RectTransform canvasRT = canvasObj.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(550, 220);

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = UISpriteFactory.GetRoundedSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.11f, 0.11f, 0.12f, 0.95f);
            
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            Shadow shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -5);

            GameObject accentObj = new GameObject("AccentLine");
            accentObj.transform.SetParent(bgObj.transform, false);
            Image accent = accentObj.AddComponent<Image>();
            accent.gameObject.SetActive(false);

            GameObject badgeObj = new GameObject("BadgeBg");
            badgeObj.transform.SetParent(bgObj.transform, false);
            Image badge = badgeObj.AddComponent<Image>();
            badge.sprite = UISpriteFactory.GetPillSprite();
            badge.type = Image.Type.Sliced;
            badge.color = new Color(1, 1, 1, 0.15f);
            
            RectTransform badgeRT = badgeObj.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0, 1); badgeRT.anchorMax = new Vector2(0, 1);
            badgeRT.pivot = new Vector2(0, 1);
            badgeRT.anchoredPosition = new Vector2(25, -20);
            badgeRT.sizeDelta = new Vector2(150, 30);

            GameObject typeObj = new GameObject("TypeText");
            typeObj.transform.SetParent(badgeObj.transform, false);
            TextMeshProUGUI type = typeObj.AddComponent<TextMeshProUGUI>();
            type.text = "NETWORK NODE";
            type.fontSize = 14;
            type.fontStyle = FontStyles.Bold;
            type.alignment = TextAlignmentOptions.Center;
            type.color = Color.white;
            
            RectTransform typeRT = typeObj.GetComponent<RectTransform>();
            typeRT.anchorMin = Vector2.zero; typeRT.anchorMax = Vector2.one;
            typeRT.sizeDelta = Vector2.zero;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            title.text = "Identificador del Nodo";
            title.fontSize = 32;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Left;
            title.color = Color.white;
            
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0, 1);
            titleRT.anchoredPosition = new Vector2(25, -60);
            titleRT.sizeDelta = new Vector2(-40, 45);

            GameObject detailsObj = new GameObject("Details");
            detailsObj.transform.SetParent(bgObj.transform, false);
            TextMeshProUGUI details = detailsObj.AddComponent<TextMeshProUGUI>();
            details.text = "Latitud:\nLongitud:\nDistancia:";
            details.fontSize = 18;
            details.color = new Color(0.7f, 0.75f, 0.8f, 1f);
            details.alignment = TextAlignmentOptions.TopLeft;
            
            RectTransform detailsRT = detailsObj.GetComponent<RectTransform>();
            detailsRT.anchorMin = new Vector2(0, 0); detailsRT.anchorMax = new Vector2(1, 0);
            detailsRT.pivot = new Vector2(0, 0);
            detailsRT.anchoredPosition = new Vector2(25, 20);
            detailsRT.sizeDelta = new Vector2(-40, 95);
            
            GameObject crewBtnObj = new GameObject("CrewButton");
            crewBtnObj.transform.SetParent(bgObj.transform, false);
            Image crewBtnImg = crewBtnObj.AddComponent<Image>();
            crewBtnImg.sprite = UISpriteFactory.GetPillSprite();
            crewBtnImg.type = Image.Type.Sliced;
            crewBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button cBtn = crewBtnObj.AddComponent<Button>();

            RectTransform cBtnRT = crewBtnObj.GetComponent<RectTransform>();
            cBtnRT.anchorMin = new Vector2(1, 0); cBtnRT.anchorMax = new Vector2(1, 0);
            cBtnRT.pivot = new Vector2(1, 0);
            cBtnRT.anchoredPosition = new Vector2(-280, 20);
            cBtnRT.sizeDelta = new Vector2(120, 40);

            GameObject cBtnTextObj = new GameObject("CBtnText");
            cBtnTextObj.transform.SetParent(crewBtnObj.transform, false);
            TextMeshProUGUI cBtnText = cBtnTextObj.AddComponent<TextMeshProUGUI>();
            cBtnText.text = "Crew Info";
            cBtnText.fontSize = 14;
            cBtnText.fontStyle = FontStyles.Bold;
            cBtnText.alignment = TextAlignmentOptions.Center;
            cBtnText.color = Color.white;
            RectTransform cBtnTextRT = cBtnTextObj.GetComponent<RectTransform>();
            cBtnTextRT.anchorMin = Vector2.zero; cBtnTextRT.anchorMax = Vector2.one;
            cBtnTextRT.sizeDelta = Vector2.zero;

            GameObject machBtnObj = new GameObject("MachineryButton");
            machBtnObj.transform.SetParent(bgObj.transform, false);
            Image machBtnImg = machBtnObj.AddComponent<Image>();
            machBtnImg.sprite = UISpriteFactory.GetPillSprite();
            machBtnImg.type = Image.Type.Sliced;
            machBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button mBtn = machBtnObj.AddComponent<Button>();

            RectTransform mBtnRT = machBtnObj.GetComponent<RectTransform>();
            mBtnRT.anchorMin = new Vector2(1, 0); mBtnRT.anchorMax = new Vector2(1, 0);
            mBtnRT.pivot = new Vector2(1, 0);
            mBtnRT.anchoredPosition = new Vector2(-150, 20);
            mBtnRT.sizeDelta = new Vector2(120, 40);

            GameObject mBtnTextObj = new GameObject("MBtnText");
            mBtnTextObj.transform.SetParent(machBtnObj.transform, false);
            TextMeshProUGUI mBtnText = mBtnTextObj.AddComponent<TextMeshProUGUI>();
            mBtnText.text = "Maquinaria";
            mBtnText.fontSize = 14;
            mBtnText.fontStyle = FontStyles.Bold;
            mBtnText.alignment = TextAlignmentOptions.Center;
            mBtnText.color = Color.white;
            RectTransform mBtnTextRT = mBtnTextObj.GetComponent<RectTransform>();
            mBtnTextRT.anchorMin = Vector2.zero; mBtnTextRT.anchorMax = Vector2.one;
            mBtnTextRT.sizeDelta = Vector2.zero;

            GameObject permitBtnObj = new GameObject("PermitButton");
            permitBtnObj.transform.SetParent(bgObj.transform, false);
            Image permitBtnImg = permitBtnObj.AddComponent<Image>();
            permitBtnImg.sprite = UISpriteFactory.GetPillSprite();
            permitBtnImg.type = Image.Type.Sliced;
            permitBtnImg.color = new Color(0f, 0.48f, 1f, 1f);
            Button pBtn = permitBtnObj.AddComponent<Button>();

            RectTransform pBtnRT = permitBtnObj.GetComponent<RectTransform>();
            pBtnRT.anchorMin = new Vector2(1, 0); pBtnRT.anchorMax = new Vector2(1, 0);
            pBtnRT.pivot = new Vector2(1, 0);
            pBtnRT.anchoredPosition = new Vector2(-20, 20);
            pBtnRT.sizeDelta = new Vector2(120, 40);

            GameObject pBtnTextObj = new GameObject("PBtnText");
            pBtnTextObj.transform.SetParent(permitBtnObj.transform, false);
            TextMeshProUGUI pBtnText = pBtnTextObj.AddComponent<TextMeshProUGUI>();
            pBtnText.text = "View Permits";
            pBtnText.fontSize = 14;
            pBtnText.fontStyle = FontStyles.Bold;
            pBtnText.alignment = TextAlignmentOptions.Center;
            pBtnText.color = Color.white;
            RectTransform pBtnTextRT = pBtnTextObj.GetComponent<RectTransform>();
            pBtnTextRT.anchorMin = Vector2.zero; pBtnTextRT.anchorMax = Vector2.one;
            pBtnTextRT.sizeDelta = Vector2.zero;

            GameObject hideBtnObj = new GameObject("HideButton");
            hideBtnObj.transform.SetParent(bgObj.transform, false);
            Image hideBtnImg = hideBtnObj.AddComponent<Image>();
            hideBtnImg.sprite = UISpriteFactory.GetPillSprite();
            hideBtnImg.type = Image.Type.Sliced;
            hideBtnImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            Button hBtn = hideBtnObj.AddComponent<Button>();

            RectTransform hBtnRT = hideBtnObj.GetComponent<RectTransform>();
            hBtnRT.anchorMin = new Vector2(1, 0); hBtnRT.anchorMax = new Vector2(1, 0);
            hBtnRT.pivot = new Vector2(1, 0);
            hBtnRT.anchoredPosition = new Vector2(-20, -30); // Below Permit Button
            hBtnRT.sizeDelta = new Vector2(120, 40);

            GameObject hBtnTextObj = new GameObject("HBtnText");
            hBtnTextObj.transform.SetParent(hideBtnObj.transform, false);
            TextMeshProUGUI hBtnText = hBtnTextObj.AddComponent<TextMeshProUGUI>();
            hBtnText.text = "OCULTAR";
            hBtnText.fontSize = 14;
            hBtnText.fontStyle = FontStyles.Bold;
            hBtnText.alignment = TextAlignmentOptions.Center;
            hBtnText.color = Color.white;
            RectTransform hBtnTextRT = hBtnTextObj.GetComponent<RectTransform>();
            hBtnTextRT.anchorMin = Vector2.zero; hBtnTextRT.anchorMax = Vector2.one;
            hBtnTextRT.sizeDelta = Vector2.zero;

            GameObject closeBtnObj = new GameObject("CloseMainCardBtn");
            closeBtnObj.transform.SetParent(bgObj.transform, false);
            Image closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.sprite = UISpriteFactory.GetRoundedSprite();
            closeBtnImg.type = Image.Type.Sliced;
            closeBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button clBtn = closeBtnObj.AddComponent<Button>();

            RectTransform clBtnRT = closeBtnObj.GetComponent<RectTransform>();
            clBtnRT.anchorMin = new Vector2(1, 1); clBtnRT.anchorMax = new Vector2(1, 1);
            clBtnRT.pivot = new Vector2(1, 1);
            clBtnRT.anchoredPosition = new Vector2(-10, -10);
            clBtnRT.sizeDelta = new Vector2(30, 30);

            GameObject clBtnTextObj = new GameObject("ClBtnText");
            clBtnTextObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI clBtnText = clBtnTextObj.AddComponent<TextMeshProUGUI>();
            clBtnText.text = "X";
            clBtnText.fontSize = 18;
            clBtnText.fontStyle = FontStyles.Bold;
            clBtnText.alignment = TextAlignmentOptions.Center;
            clBtnText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform clBtnTextRT = clBtnTextObj.GetComponent<RectTransform>();
            clBtnTextRT.anchorMin = Vector2.zero; clBtnTextRT.anchorMax = Vector2.one;
            clBtnTextRT.sizeDelta = Vector2.zero;

            GameObject scaleBtnObj = new GameObject("ScaleMainCardBtn");
            scaleBtnObj.transform.SetParent(bgObj.transform, false);
            Image scaleBtnImg = scaleBtnObj.AddComponent<Image>();
            scaleBtnImg.sprite = UISpriteFactory.GetRoundedSprite();
            scaleBtnImg.type = Image.Type.Sliced;
            scaleBtnImg.color = new Color(0.17f, 0.17f, 0.18f, 1f);
            Button scBtn = scaleBtnObj.AddComponent<Button>();

            RectTransform scBtnRT = scaleBtnObj.GetComponent<RectTransform>();
            scBtnRT.anchorMin = new Vector2(1, 1); scBtnRT.anchorMax = new Vector2(1, 1);
            scBtnRT.pivot = new Vector2(1, 1);
            scBtnRT.anchoredPosition = new Vector2(-50, -10);
            scBtnRT.sizeDelta = new Vector2(30, 30);

            GameObject scBtnTextObj = new GameObject("ScBtnText");
            scBtnTextObj.transform.SetParent(scaleBtnObj.transform, false);
            TextMeshProUGUI scBtnText = scBtnTextObj.AddComponent<TextMeshProUGUI>();
            scBtnText.text = "S";
            scBtnText.fontSize = 16;
            scBtnText.fontStyle = FontStyles.Bold;
            scBtnText.alignment = TextAlignmentOptions.Center;
            scBtnText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            RectTransform scBtnTextRT = scBtnTextObj.GetComponent<RectTransform>();
            scBtnTextRT.anchorMin = Vector2.zero; scBtnTextRT.anchorMax = Vector2.one;
            scBtnTextRT.sizeDelta = Vector2.zero;

            NodeInfoCardUI uiScript = canvasObj.AddComponent<NodeInfoCardUI>();
            uiScript.titleText = title;
            uiScript.typeText = type;
            uiScript.detailsText = details;
            uiScript.accentLine = accent;
            uiScript.badgeBg = badge;
            uiScript.crewButton = cBtn;
            uiScript.machineryButton = mBtn;
            uiScript.permitButton = pBtn;
            uiScript.hideButton = hBtn;
            
            cBtn.onClick.AddListener(uiScript.OnCrewButtonClicked);
            mBtn.onClick.AddListener(uiScript.OnMachineryButtonClicked);
            pBtn.onClick.AddListener(uiScript.OnPermitButtonClicked);
            clBtn.onClick.AddListener(uiScript.HideCard);
            scBtn.onClick.AddListener(uiScript.ToggleSize);

            UIXRUtils.SetLayerRecursively(canvasObj, 5);
            UIXRUtils.MakeGrabbable(canvasObj, canvasRT.sizeDelta);

            canvasObj.SetActive(false);
            return uiScript;
        }
    }
}
