using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

/**
 *   版权：博毅创为教育咨询有限公司  2018
 *   作者：Felix老师
 *   主键: key, ID, 如果没有用索引，从0开始
 */
public class ExcelDataMgr : Singleton<ExcelDataMgr>
{
	Dictionary<string,Dictionary<string, object>> dataDic = null;

	public void Init() {
		this.dataDic = new Dictionary<string, Dictionary<string, object>>();
	}

	public T GetConfigData<T>(string key, string fileName = null)  {
		Type setT = typeof(T);
		if (fileName == null) {
			fileName = setT.Name;
		}

		if (!dataDic.ContainsKey(fileName)) {
			ReadConfigData<T>(fileName);
		}

		Dictionary<string, object> objDic = dataDic[fileName];
		// Debug.Log("test  (" + key + ")" + objDic.Count);
		if (!objDic.ContainsKey (key)) {
			throw new Exception ("no this config");
		}
		return (T)(objDic [key]);
	}

	public List<T> GetConfigDatas<T>(string fileName, bool isPriKey = true) 
	{
		List<T> returnList = new List<T> ();
		Type setT = typeof(T);
		if (fileName == null) {
			fileName = setT.Name;
		}

        if (!dataDic.ContainsKey(fileName)) {
			ReadConfigData<T> (fileName, isPriKey);
		}

		Dictionary<string, object> objDic = dataDic[fileName];
		foreach (KeyValuePair<string, object> kvp in objDic) {
			returnList.Add ((T)(kvp.Value));
		}
		return returnList;
	}

	public void ReadConfigData<T>(string fileName = null, bool hasPriKey = true) {
		if (fileName == null) {
			fileName = typeof(T).Name;
		}
		
		string path = "Datas/" + fileName + ".csv";
        string getString = (ResMgr.Instance.LoadAssetSync<TextAsset>(path) as TextAsset).text;
		CsvReaderByString csr = new CsvReaderByString (getString);

		Dictionary<string,object> objDic = new Dictionary<string, object> ();
				
		FieldInfo[] fis = new FieldInfo[csr.ColCount];
		for (int colNum=1; colNum<csr.ColCount+1; colNum++) {
			fis [colNum - 1] = typeof(T).GetField (csr [3, colNum]);
		}

		int index = 0;
		for (int rowNum=4; rowNum<csr.RowCount+1; rowNum++) {
			T configObj = Activator.CreateInstance<T> ();
			for (int i=0; i<fis.Length; i++) {
				string fieldValue = csr [rowNum, i + 1];
				object setValue = new object ();

				switch (fis [i].FieldType.ToString ()) {
					case "System.Int32":
						setValue = int.Parse (fieldValue);
					break;
					case "System.Int64":
						setValue = long.Parse (fieldValue);
					break;
					case "System.String":
						setValue = fieldValue;
					break;
					case "System.Single":
						try {
							setValue = float.Parse(fieldValue);
						}
						catch (System.Exception e) {
							setValue = 0.0f;
						}

					break;
					default:
						Debug.Log ("error data type: " + fis[i].FieldType.ToString());
						break;
				}
				fis[i].SetValue (configObj, setValue);
				if (hasPriKey && (fis[i].Name == "key" || fis [i].Name == "ID")) {
					//只检测key和id的值，然后添加到objDic 中
					objDic.Add (setValue.ToString (), configObj);
				}
			}

			if (!hasPriKey) {
				objDic.Add(index.ToString(), configObj);
			}
			index++;
		}
		dataDic.Add(fileName, objDic);    //可以作为参数
	}
}
