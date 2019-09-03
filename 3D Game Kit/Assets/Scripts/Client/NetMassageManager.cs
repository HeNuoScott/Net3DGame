using QFramework;
using QFramework.HeNuoApp;

public class NetMassageManager
{
    /// <summary>
    /// 打开提示消息面板
    /// </summary>
    /// <param name="information"></param>
    public static void OpenMessage(string information)
    {
        UIMgr.OpenPanel<MassagePanel>(UILevel.PopUI, new MassagePanelData() { info = information });
    }
    /// <summary>
    /// 打开顶端消息提示
    /// </summary>
    /// <param name="information"></param>
    public static void OpenTitleMessage(string information)
    {
        //UIMgr.ClosePanel<TitleMassagePanel>();
        UIMgr.OpenPanel<TitleMassagePanel>(UILevel.Forward, new TitleMassagePanelData() { info = information });
    }
}
