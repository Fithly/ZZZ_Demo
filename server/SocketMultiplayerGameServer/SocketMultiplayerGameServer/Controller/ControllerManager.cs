using System;
using System.Collections.Generic;
using System.Reflection;
using SocketGameProtocol;
using SocketMultiplayerGameServer.Servers;

namespace SocketMultiplayerGameServer.Controller
{
    class ControllerManager
    {
        private Dictionary<RequestCode, BaseController> controlDict = new Dictionary<RequestCode, BaseController>();
        private Server server;

        // 控制器管理类在服务器中初始化 并得到该服务器
        public ControllerManager(Server server)
        {
            this.server = server;

            // 用户登录/注册
            UserController userController = new UserController();
            controlDict.Add(userController.GetRequestCode, userController);

            // 房间操作
            RoomController roomController = new RoomController();
            controlDict.Add(roomController.GetRequestCode, roomController);

            // 注册游戏 / 战斗控制器
            GameController gameController = new GameController();
            controlDict.Add(gameController.GetRequestCode, gameController);
        }

        // 处理请求
        public void HandleRequest(MainPack pack, Client client)
        {
            if (pack == null)
                return;

            if (controlDict.TryGetValue(pack.Requestcode, out BaseController controller))
            {
                // 得到名字
                string metname = pack.Actioncode.ToString();

                // 修改：BattleState / BattleInput 是高频消息，不再刷屏输出
                if (pack.Actioncode != ActionCode.BattleState &&
                    pack.Actioncode != ActionCode.BattleInput)
                {
                    Console.WriteLine(metname);
                }

                // 对应Controller中的方法名 需要与Actioncode一致
                MethodInfo method = controller.GetType().GetMethod(metname);
                if (method == null)
                {
                    Console.WriteLine("没有找到对应的处理方法：" + metname);
                    return;
                }

                object[] obj = new object[] { server, client, pack };

                try
                {
                    // 在controller中调用方法
                    object result = method.Invoke(controller, obj);

                    if (result != null)
                    {
                        client.Send(result as MainPack);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("处理请求异常：" + metname + " " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("没有找到对应的controller处理：" + pack.Requestcode);
            }
        }
    }
}