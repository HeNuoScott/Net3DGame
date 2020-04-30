using UnityEngine;

namespace Server
{
    public class Monster
    {
        private int mId;
        private Vector3Int mPosition;
        private Vector3Int mDirection;

        public int Id { get { return mId; } }
        public Vector3Int Position { get { return mPosition; } set { mPosition = value; } }
        public Vector3Int Direction { get { return mDirection; } set { mDirection = value; } }

        public LifeEntity mLifeEntity = new LifeEntity();

        public Monster(int monsterId)
        {
            mId = monsterId;

            mLifeEntity.roleid = mId;
            mLifeEntity.name = mId.ToString();
            mLifeEntity.moveSpeed = 500;
            mLifeEntity.moveSpeedAddition = 0;
            mLifeEntity.moveSpeedPercent = 0;
            mLifeEntity.attackSpeed = 100;
            mLifeEntity.attackSpeedAddition = 0;
            mLifeEntity.attackSpeedPercent = 0;
            mLifeEntity.maxBlood = 100;
            mLifeEntity.nowBlood = 100;
            mLifeEntity.type = 1;//怪物
        }
    }
}
