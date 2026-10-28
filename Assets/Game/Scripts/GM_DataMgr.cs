using Game.Datas.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GM_DataMgr : Singleton<GM_DataMgr>
{
    public const string GUEST_KEY = "BYCW_SLG_GUEST_KEY";
    public string guestKey = null;

    public AccountInfo account = null;

    public void Init() {
        // 下一次登录的时候，我们还是要之前的游客key;
        this.guestKey = PlayerPrefs.GetString(GUEST_KEY, null);
    }


    public void SaveGuestKey(string guestKey) {
        PlayerPrefs.SetString(GUEST_KEY, guestKey);
        this.guestKey = guestKey;
    }
}
