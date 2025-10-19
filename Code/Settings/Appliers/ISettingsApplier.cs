namespace Nonsliep.Core.Settings
{
    // 공통 초기화 모듈 인터페이스
    public interface ISettingsApplier
    {
        // 이벤트 구독 등 1회 초기화 작업
        void Initialize();

        // 현재 저장된 값으로 외부 시스템에 1회 적용
        void ApplyInitial();

        // 구독 해제 등 정리 작업
        void Cleanup();
    }
}

