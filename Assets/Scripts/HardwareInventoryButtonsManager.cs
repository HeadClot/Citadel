using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HardwareInventoryButtonsManager : MonoBehaviour {
	public GameObject[] hwButtons;

	public void ActivateHardwareButton (int index) {
		hwButtons[index].SetActive(true);
	}
	//void Update() {
	//	for (int i=0; i<14; i++) {
	//		if (HardwareInventory.a.hasHardware[i]) {
	//			if (!hwButtons[i].activeInHierarchy) hwButtons[i].SetActive(true);
	//		} else {
	//			if (hwButtons[i].activeInHierarchy) hwButtons[i].SetActive(false);
	//		}
	//	}
	//}
}
