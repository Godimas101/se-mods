using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using static VRageRender.MyBillboard;

namespace APEX.Advanced.MyMedicalRoom
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MedicalRoom), false, new string[]
    { "LargeMedicalRoom", "LargeMedicalRoomReskin" })]
    public class MedicalRoomHealNerf : MyGameLogicComponent
    {
        private IMyMedicalRoom m_medicalRoom;
        private int tick;

        private double radius = 3;
        private Vector3D center;
        private BoundingSphereD detectionSphere;
        private List<IMyEntity> nearbyEntities;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            m_medicalRoom = Entity as IMyMedicalRoom;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            tick++;

            if(Debug.IS_DEBUG && Debug.Level == DebugLevel.Debug)
            {
                center = m_medicalRoom.PositionComp.GetPosition();
                detectionSphere = new BoundingSphereD(center, radius);
                DrawSphere(detectionSphere, Color.Red);
            }

            if (tick % 100 != 0)
                return;

            if (!m_medicalRoom.IsWorking)
                return;

            center = m_medicalRoom.PositionComp.GetPosition();
            detectionSphere = new BoundingSphereD(center, radius);

            // should be fast enought to not get parallel problems....
            nearbyEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref detectionSphere);
            foreach (var entity in nearbyEntities)
            {
                IMyCharacter character = entity as IMyCharacter;
                if (character != null && !character.Closed && !character.MarkedForClose && character.IsPlayer && !character.IsBot)
                {
                    if ((character.OxygenLevel > 0.5f && character.SuitEnergyLevel > 0.99f) || ConfigManager.Config.MedicalRoomWorksWithoutOxygenToHeal)
                    {
                        MyCharacterStatComponent _statComponent = character.Components.Get<MyCharacterStatComponent>();
                        MyEntityStat foundStat;
                        
                        if (_statComponent.TryGetStat(MyStringHash.GetOrCompute("Health"), out foundStat) && foundStat != null)
                        {
                            // cap healing                            
                            float _maxHealthValue = foundStat.MaxValue * ConfigManager.Config.MedicalRoomCanHealUpToPercent;
                            float _curHealthValue = foundStat.Value;
                            
                            // skip if health is already above 
                            if (_curHealthValue >= _maxHealthValue) 
                                continue;
                            
                            // heal
                            if ((_curHealthValue + ConfigManager.Config.MedicalRoomRegenerationPer100Ticks) > _maxHealthValue)
                                foundStat.Value = _maxHealthValue;
                            else
                                foundStat.Value = _curHealthValue + ConfigManager.Config.MedicalRoomRegenerationPer100Ticks;
                        }
                    }
                }
            }
        }

        private void DrawSphere(BoundingSphereD sphere, Color color, MySimpleObjectRasterizer draw = MySimpleObjectRasterizer.SolidAndWireframe, BlendTypeEnum blend = BlendTypeEnum.PostPP)
        {
            MatrixD wm = MatrixD.CreateTranslation(sphere.Center);
            MySimpleObjectDraw.DrawTransparentSphere(ref wm, (float)sphere.Radius, ref color, draw, 24, null, null, 0.01f, blendType: blend);
        }
    }
}