/****************************************************************************
 * 2019.9 DESKTOP-JJEQCQA
 ****************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.HeNuoApp
{
	public partial class ScenePanel
	{
		[SerializeField] public UnityEngine.UI.Text SceneName;
		[SerializeField] public UnityEngine.UI.Text capacity;
		[SerializeField] public UnityEngine.UI.Button Join_Button;

		public void Clear()
		{
			SceneName = null;
			capacity = null;
			Join_Button = null;
		}

		public override string ComponentName
		{
			get { return "ScenePanel";}
		}
	}
}
