using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMgr : UnitySingleton<SceneMgr>
{
    public void Init() {
        
    }

    public void EnterScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }

    public async void RunScene(string sceneName)
    {
        YooAsset.SceneOperationHandle h = ResMgr.Instance.LoadSceneAsync(sceneName);
        await h.Task;
    }

    public IEnumerator IE_RunScene(string sceneName) {
     
        YooAsset.SceneOperationHandle h = ResMgr.Instance.LoadSceneAsync(sceneName);
        yield return h;
    }
}
