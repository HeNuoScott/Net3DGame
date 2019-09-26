#开发者说明
#Unity版本2018.4.8
#QFramework 0.1.11
	Framework v0.1.15
	1.core v0.02
	2.ResKit v0.18
	3.UIKit v0.5.5
	
	框架中的方法修改
	UIManager中的方法替换为
	public void CloseAllUI()
	{
         foreach (var layer in mAllUI)
         {
                if((layer.Value as UIPanel))
                {
                    layer.Value.Close();
                }
         Destroy(layer.Value.Transform.gameObject);
         }
         mAllUI.Clear();
	}
#GameDesigner 2019.9.18
	NetScene 类中添加字段
	public string sceneName;
	
目前所做结果说明
	所有系统均已组建化开发 方便解耦
	unity发布 Server和Client 在根目录Bulid文件加下 可直接运行
