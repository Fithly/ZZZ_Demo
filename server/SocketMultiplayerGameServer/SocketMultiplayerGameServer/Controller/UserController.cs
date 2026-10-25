using SocketGameProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketMultiplayerGameServer.Servers;

namespace SocketMultiplayerGameServer.Controller
{
    class UserController : BaseController
    {
        public UserController()
        {
            requestCode = RequestCode.User;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <returns></returns>
        public MainPack Logon(Server server, Client client, MainPack pack) // 类型和消息主体名相同
        {
            // 处理内容在Server中实现 低耦合
            if(client.GetUserData.Logon(pack, client.GetMySqlConnect))
            {
                pack.Returncode = ReturnCode.Succeed;
            }
            else
            {
                pack.Returncode = ReturnCode.Fail;
            }
            return pack;
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        public MainPack Login(Server server, Client client, MainPack pack)
        {
            if (client.GetUserData.Login(pack, client.GetMySqlConnect))
            {
                pack.Returncode = ReturnCode.Succeed;
                // 登录成功对客户端的房间用户名进行赋值
                client.UserName = pack.Loginpack.Username;
            }
            else
            {
                pack.Returncode = ReturnCode.Fail;
            }
            return pack;
        }
    }
}
