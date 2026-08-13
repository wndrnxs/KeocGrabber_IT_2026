/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeocGrabber
{
    class LightManager
    {
        VitLight mc_VitLight = new VitLight();
        DawooLight mc_DawooLight = new DawooLight();

        public int TopLightCount { get { return mc_DawooLight.CtrlCount; } }
        public int Light_CTRL_CH_Count = 2;

        public bool this[int index] 
        { 
            get 
            {
                if (index < mc_DawooLight.CtrlCount)
                    return mc_DawooLight[index];
                else
                    return mc_VitLight.IsConnected;
            }
        }

        public LightManager()
        {

        }

        public bool fn_IsConnected(int index)
        {
            if (index < 4)
                return mc_DawooLight[index];
            else
                return mc_VitLight.IsConnected;
        }

        public void fn_Init()
        {
            mc_DawooLight.fn_InitPort();
            if(G.SYSTEM.CamCount>2)
            {
                mc_VitLight.fn_InitPort();
                Light_CTRL_CH_Count = 11;
            }
        }

        public void fn_Final()
        {
            mc_VitLight.fn_FinalPort();
            mc_DawooLight.fn_FinalPort();
        }

        public void fn_LightOnAll()
        {
            for (int i = 0; i < mc_DawooLight.CtrlCount; i++)
            {
                mc_DawooLight.LightOn(i);
            }
            mc_VitLight.LightOn(-1);
        }
        public void fn_LightOn_VIT()
        {
            mc_VitLight.LightOn(-1);
        }
        public void fn_LightOn_DAWOO()
        {
            for (int i = 0; i < mc_DawooLight.CtrlCount; i++)
            {
                mc_DawooLight.LightOn(i);
            }
        }

        public void fn_LightOffAll()
        {
            for (int i = 0; i < mc_DawooLight.CtrlCount; i++)//0-3
            {
                mc_DawooLight.LightOff(i);
            }
            mc_VitLight.LightOff(-1);//4,5, 6, 7, 8,9
        }


        //CJk. 상부cam 촬상 시 하부 light 간섭으로 인한 function 생성. 
        public void fn_Vit_ON()
        {
            mc_VitLight.LightOn(8 - mc_DawooLight.CtrlCount);
            mc_VitLight.LightOn(9 - mc_DawooLight.CtrlCount);
        }

        public void fn_LightOn(int idx)
        {
            if (idx >= 0 && idx < Light_CTRL_CH_Count)
            {
                if (idx < mc_DawooLight.CtrlCount)
                {
                    mc_DawooLight.LightOn(idx);
                }
                else
                {
                    mc_VitLight.LightOn(idx - mc_DawooLight.CtrlCount);
                }
            }
        }

        public void fn_LightOff(int idx)
        {
            if (idx >= 0 && idx < Light_CTRL_CH_Count)
            {
                if (idx < mc_DawooLight.CtrlCount)
                {
                    mc_DawooLight.LightOff(idx);
                }
                else
                {
                    mc_VitLight.LightOff(idx - mc_DawooLight.CtrlCount);
                }
            }
        }

        public void fn_SetLightValue(int idx, int value)
        {
            if (idx >= 0 && idx < Light_CTRL_CH_Count)
            {
                if (idx < mc_DawooLight.CtrlCount)
                {
                    mc_DawooLight.SetLightValue(idx, value);
                }
                else
                {
                    mc_VitLight.SetLightValue(idx - mc_DawooLight.CtrlCount + 1, value);
                }
            }
        }

        public int? fn_GetLightValue(int idx)
        {
            int? nRtn = 0;
            if (idx >= 0 && idx < Light_CTRL_CH_Count)
            {
                if (idx < mc_DawooLight.CtrlCount)
                {
                    nRtn = mc_DawooLight.GetLightValue(idx, 0);
                }
                else
                {
                    nRtn = mc_VitLight.GetLightValue(idx - mc_DawooLight.CtrlCount + 1);
                }
            }

            return nRtn;
        }

        public bool fn_IsLightOn(int idx)
        {
            bool bRet = false;
            if (idx >= 0 && idx < Light_CTRL_CH_Count)
            {
                if (idx < mc_DawooLight.CtrlCount)
                {
                    bRet = mc_DawooLight.IsLightOn(idx);
                }
                else
                {
                    bRet = mc_VitLight.IsLightOn(idx - mc_DawooLight.CtrlCount);
                }
            }

            return bRet;
        }

        public bool fn_GetOnOff(int idx)
        {
            bool bRet = false;
            if (idx >= 0 && idx < Light_CTRL_CH_Count)
            {
                if (idx < mc_DawooLight.CtrlCount)
                {
                    mc_DawooLight.GetLightOnOff(idx);
                }
                else
                {
                    mc_VitLight.GetLightOnOff();
                }
            }

            return bRet;
        }
    }
}
