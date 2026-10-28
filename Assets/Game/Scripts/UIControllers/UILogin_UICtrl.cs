using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class UILogin_UICtrl : UICtrl {

	void Start() {
		this.AddButtonListener("GuestLoginBtn", this.OnGuestLoginClick);
	}

	private void OnGuestLoginClick() {
		EventMgr.Instance.Emit((int)EventType.UI, UIEvent.UIGuestLogin);
	}

}
