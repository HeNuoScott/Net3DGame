
教程：https://www.bilibili.com/video/BV13E411k75W
Proto官网地址：https://developers.google.cn/protocol-buffers/
ProtoBuf使用说明
1. 启动  protobuf-3.11.4\csharp\src\Google.Protobuf.sln   VS生成dll Google.Protobuf.dll
2.编写bat

for /f "delims=" %%i in ('dir /b proto "proto/*.proto"') do protoc -I=proto/ --csharp_out=cs/ proto/%%i 
pause

3.编写proto协议用 编写好的bat转换为指定的语言类型


proto文件转换为cs脚本插件，使用时运行run.bat，将要转化的proto文件放入proto文件夹下