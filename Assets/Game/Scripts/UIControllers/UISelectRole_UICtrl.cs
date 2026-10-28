using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Game.Datas.Messages;

public class UISelectRole_UICtrl : UICtrl {

	private int usex = 0;
	private int job = 1;
	private string unick = "Bycw" + DataUtils.RandString(5);
	private int charactorId = 0; // Œ‰ ø0£¨Œ‰ ø1;

	void Start() {

		this.View<InputField>("unick").text = unick;

		this.AddButtonListener("Sex/male", ()=> {
			this.OnSexClick(0);
		});

		this.AddButtonListener("Sex/womale", () => {
			this.OnSexClick(1);
		});


		this.AddButtonListener("Jobs/wushi", () => {
			this.OnJobSelect(1);
		});

		this.AddButtonListener("Jobs/zhanshi", () => {
			this.OnJobSelect(2);
		});

		this.AddButtonListener("Jobs/fashi", () => {
			this.OnJobSelect(3);
		});

		this.AddButtonListener("CommitBtn", this.OnCommitClick);
	}

	private void OnJobSelect(int job) {

		Vector3 pos = this.ViewNode("Jobs/tip").transform.localPosition;
		if (job == 0) {
			this.job = job;
			pos.x = -300;
			this.ViewNode("Jobs/tip").transform.localPosition = pos;
		}
		else if (job == 1) {
			this.job = job;
			pos.x = 0;
			this.ViewNode("Jobs/tip").transform.localPosition = pos;
		}
		else if (job == 2) {
			this.job = job;
			pos.x = 300;
			this.ViewNode("Jobs/tip").transform.localPosition = pos;
		}
	}

	private void OnSexClick(int usex) {
		this.usex = usex;
		Vector3 pos = this.ViewNode("Sex/tip").transform.localPosition;
		if (this.usex == 0)
		{
			pos.x = -200;
		}
		else if (this.usex == 1) {
			pos.x = 200;
		}

		this.ViewNode("Sex/tip").transform.localPosition = pos;
	}

	private void OnCommitClick() {
		ReqSelectPlayer req = new ReqSelectPlayer();
		req.charactorId = this.charactorId;
		req.job = this.job;
		req.uname = this.View<InputField>("unick").text;
		req.usex = this.usex;

		EventMgr.Instance.Emit((int)EventType.UI, UIEvent.UISelectRole, req);
	}

}
