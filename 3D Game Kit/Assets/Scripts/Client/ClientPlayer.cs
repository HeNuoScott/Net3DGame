using UnityEngine;
using Net.Share;

namespace Client
{
    public class ClientPlayer : Net.Client.NetBehaviour
    {
        public string playerName;
        public string acc, pass;
        public float speed = 10;
        public Vector3 pos;
        public Quaternion roto;

        private void Start()
        {
            if (ClientNetworkManager.Instance.playerName != playerName)
            {
                enabled = false;
            }
        }

        public void Update()
        {
            transform.Translate(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * speed * Time.deltaTime);
            if (pos != transform.position)
            {
                pos = transform.position;
                Send(NetCmd.SceneCmd, "InputMove", playerName, transform.position, transform.rotation);
            }
            
        }
        [Net.Share.Rpc]
        private void InputMove(string playerName,Vector3 pos,Quaternion roto)
        {
            if (playerName==this.playerName)
            {
                transform.position = pos;
                transform.rotation = roto;
            }
        }
    }
}