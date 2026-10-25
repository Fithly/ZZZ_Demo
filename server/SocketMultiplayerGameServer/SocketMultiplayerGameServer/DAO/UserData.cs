using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SocketGameProtocol;

namespace SocketMultiplayerGameServer.DAO
{
    class UserData
    {
        // 注册（参数：消息）
        public bool Logon(MainPack pack, MySqlConnection sqlConnection)
        {
            string username = pack.Loginpack.Username;
            string password = pack.Loginpack.Password;

            try
            {
                // 先检查用户名是否已存在
                string checkSql = "SELECT COUNT(*) FROM userdata WHERE username = @username";
                using (MySqlCommand checkCmd = new MySqlCommand(checkSql, sqlConnection))
                {
                    checkCmd.Parameters.AddWithValue("@username", username);
                    long count = (long)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        Console.WriteLine($"用户名 '{username}' 已存在！");
                        return false;
                    }
                }

                // 插入新用户（使用参数化查询防止SQL注入）
                string insertSql = "INSERT INTO userdata (username, password) VALUES (@username, @password)";
                using (MySqlCommand insertCmd = new MySqlCommand(insertSql, sqlConnection))
                {
                    insertCmd.Parameters.AddWithValue("@username", username);
                    insertCmd.Parameters.AddWithValue("@password", password);

                    int rowsAffected = insertCmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册失败：{ex.Message}");
                return false;
            }
        }

        // 登录
        public bool Login(MainPack pack, MySqlConnection sqlConnection)
        {
            string username = pack.Loginpack.Username;
            string password = pack.Loginpack.Password;

            // 1. 检查用户名和密码是否匹配（使用参数化查询防止SQL注入）
            string checkSql = "SELECT COUNT(*) FROM userdata WHERE username = @username AND password = @password";

            using (MySqlCommand checkCmd = new MySqlCommand(checkSql, sqlConnection))
            {
                // 绑定参数
                checkCmd.Parameters.AddWithValue("@username", username);
                checkCmd.Parameters.AddWithValue("@password", password);

                try
                {
                    // 执行查询并获取匹配的记录数
                    long count = (long)checkCmd.ExecuteScalar();

                    // 2. 根据结果返回登录状态
                    if (count > 0)
                    {
                        Console.WriteLine($"用户 [{username}] 登录成功！");
                        return true; // 登录成功
                    }
                    else
                    {
                        Console.WriteLine($"用户 [{username}] 登录失败：用户名或密码错误！");
                        return false; // 登录失败
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"登录查询异常：{ex.Message}");
                    return false; // 出现异常，登录失败
                }
            }
        }
    }
}