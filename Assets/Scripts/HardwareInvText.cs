using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class HardwareInvText : MonoBehaviour {
	Text text;
	public int slotnum = 0;
    public int referenceIndex = -1;
	
	void Start () {
		text = GetComponent<Text>();
	}

	void Update () {
        //referenceIndex = gameObject.transform.GetComponentInParent<HardwareInvButton>().useableItemIndex;
        //if (referenceIndex > -1) {
            text.text = Const.a.useableItemsNameText[referenceIndex];
        //} else {
        //    text.text = string.Empty;
        //}

		if (slotnum == HardwareInvCurrent.a.hardwareInvCurrent) {
			text.color = Const.a.ssYellowText; // Yellow
		} else {
			text.color = Const.a.ssGreenText; // Green
		}
	}
}