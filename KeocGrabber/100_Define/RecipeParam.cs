/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeocGrabber
{
    public class RecipeParam : MVVMBase.IPropertyChanged
    {
        string strRecipeName = "";
        string strRecipeWriteTime = "";
        public string RecipeName { get { return strRecipeName; } set { strRecipeName = value; OnPropertyChanged(nameof(RecipeName)); } }
        public string RecipeWriteTime { get { return strRecipeWriteTime; } set { strRecipeWriteTime = value; OnPropertyChanged(nameof(RecipeWriteTime)); } }

        float nCamExposure1 = 0;
        float nCamExposure2 = 0;
        float nCamExposure3 = 0;
        float nCamExposure4 = 0;

        float nCamGain1 = 0;
        float nCamGain2 = 0;
        float nCamGain3 = 0;
        float nCamGain4 = 0;

        int nLightTop1 = 0;
        int nLightTop2 = 0;
        int nLightTop3 = 0;
        int nLightTop4 = 0;

        int nLightBot1 = 0;
        int nLightBot2 = 0;
        int nLightBot3 = 0;
        int nLightBot4 = 0;
        int nLightBot5 = 0;
        int nLightBot6 = 0;

        public float CamExposure1 { get { return nCamExposure1; } set { nCamExposure1 = value; OnPropertyChanged(nameof(CamExposure1)); } }
        public float CamExposure2 { get { return nCamExposure2; } set { nCamExposure2 = value; OnPropertyChanged(nameof(CamExposure2)); } }
        public float CamExposure3 { get { return nCamExposure3; } set { nCamExposure3 = value; OnPropertyChanged(nameof(CamExposure3)); } }
        public float CamExposure4 { get { return nCamExposure4; } set { nCamExposure4 = value; OnPropertyChanged(nameof(CamExposure4)); } }

        public float CamGain1 { get { return nCamGain1; } set { nCamGain1 = value; OnPropertyChanged(nameof(CamGain1)); } }
        public float CamGain2 { get { return nCamGain2; } set { nCamGain2 = value; OnPropertyChanged(nameof(CamGain2)); } }
        public float CamGain3 { get { return nCamGain3; } set { nCamGain3 = value; OnPropertyChanged(nameof(CamGain3)); } }
        public float CamGain4 { get { return nCamGain4; } set { nCamGain4 = value; OnPropertyChanged(nameof(CamGain4)); } }

        public int LightTop1 { get { return nLightTop1; } set { nLightTop1 = value; OnPropertyChanged(nameof(LightTop1)); } }
        public int LightTop2 { get { return nLightTop2; } set { nLightTop2 = value; OnPropertyChanged(nameof(LightTop2)); } }
        public int LightTop3 { get { return nLightTop3; } set { nLightTop3 = value; OnPropertyChanged(nameof(LightTop3)); } }
        public int LightTop4 { get { return nLightTop4; } set { nLightTop4 = value; OnPropertyChanged(nameof(LightTop4)); } }

        public int LightBot1 { get { return nLightBot1; } set { nLightBot1 = value; OnPropertyChanged(nameof(LightBot1)); } }
        public int LightBot2 { get { return nLightBot2; } set { nLightBot2 = value; OnPropertyChanged(nameof(LightBot2)); } }
        public int LightBot3 { get { return nLightBot3; } set { nLightBot3 = value; OnPropertyChanged(nameof(LightBot3)); } }
        public int LightBot4 { get { return nLightBot4; } set { nLightBot4 = value; OnPropertyChanged(nameof(LightBot4)); } }
        public int LightBot5 { get { return nLightBot5; } set { nLightBot5 = value; OnPropertyChanged(nameof(LightBot5)); } }
        public int LightBot6 { get { return nLightBot6; } set { nLightBot6 = value; OnPropertyChanged(nameof(LightBot6)); } }

        // 카메라별 라인주기(us, Euresys). Matrox DCF의 LinePeriod와 같은 단위. 노광/게인처럼 Setup 화면에서
        // 편집하고 Save로 저장한다. 태그가 없는 구버전 레시피는 기본값(90.5us = 11049.7Hz)으로 로드된다.
        // 0 이하가 들어오면 Hz 환산이 깨지므로 최소 1us로 막는다.
        public const double DEFAULT_LINE_PERIOD_US = 90.5;
        double dCamLinePeriod1 = DEFAULT_LINE_PERIOD_US;
        double dCamLinePeriod2 = DEFAULT_LINE_PERIOD_US;
        double dCamLinePeriod3 = DEFAULT_LINE_PERIOD_US;
        double dCamLinePeriod4 = DEFAULT_LINE_PERIOD_US;

        public double CamLinePeriod1 { get { return dCamLinePeriod1; } set { dCamLinePeriod1 = Math.Max(1.0, value); OnPropertyChanged(nameof(CamLinePeriod1)); } }
        public double CamLinePeriod2 { get { return dCamLinePeriod2; } set { dCamLinePeriod2 = Math.Max(1.0, value); OnPropertyChanged(nameof(CamLinePeriod2)); } }
        public double CamLinePeriod3 { get { return dCamLinePeriod3; } set { dCamLinePeriod3 = Math.Max(1.0, value); OnPropertyChanged(nameof(CamLinePeriod3)); } }
        public double CamLinePeriod4 { get { return dCamLinePeriod4; } set { dCamLinePeriod4 = Math.Max(1.0, value); OnPropertyChanged(nameof(CamLinePeriod4)); } }

        // ── 카메라/조명 인덱스 기반 조회 (카메라 대수 1~4 가변 대응) ──────────────

        /// <summary>카메라 인덱스(0-base)별 라인주기(us)</summary>
        public double fn_GetCamLinePeriod(int idx)
        {
            switch (idx)
            {
                case 0: return dCamLinePeriod1;
                case 1: return dCamLinePeriod2;
                case 2: return dCamLinePeriod3;
                case 3: return dCamLinePeriod4;
            }
            return DEFAULT_LINE_PERIOD_US;
        }

        /// <summary>카메라 인덱스(0-base)별 라인레이트(Hz) = 1e6 / 라인주기(us). Euresys AcquisitionLineRate용.</summary>
        public double fn_GetCamLineRate(int idx)
        {
            return 1e6 / fn_GetCamLinePeriod(idx);
        }

        /// <summary>카메라 인덱스(0-base)별 게인</summary>
        public float fn_GetCamGain(int idx)
        {
            switch (idx)
            {
                case 0: return nCamGain1;
                case 1: return nCamGain2;
                case 2: return nCamGain3;
                case 3: return nCamGain4;
            }
            return 0;
        }

        /// <summary>카메라 인덱스(0-base)별 노광시간</summary>
        public float fn_GetCamExposure(int idx)
        {
            switch (idx)
            {
                case 0: return nCamExposure1;
                case 1: return nCamExposure2;
                case 2: return nCamExposure3;
                case 3: return nCamExposure4;
            }
            return 0;
        }

        /// <summary>상부 조명 인덱스(0-base)별 광량</summary>
        public int fn_GetLightTop(int idx)
        {
            switch (idx)
            {
                case 0: return nLightTop1;
                case 1: return nLightTop2;
                case 2: return nLightTop3;
                case 3: return nLightTop4;
            }
            return 0;
        }

        /// <summary>하부 조명 채널(0-base)별 광량</summary>
        public int fn_GetLightBot(int idx)
        {
            switch (idx)
            {
                case 0: return nLightBot1;
                case 1: return nLightBot2;
                case 2: return nLightBot3;
                case 3: return nLightBot4;
                case 4: return nLightBot5;
                case 5: return nLightBot6;
            }
            return 0;
        }

        DataTable dtCropROI = new DataTable();
        public DataTable CropROI { get { return dtCropROI; } set { dtCropROI = value; OnPropertyChanged(nameof(CropROI)); } }
    }
}
