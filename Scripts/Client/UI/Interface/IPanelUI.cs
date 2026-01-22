using UnityEngine;
/// <summary>
/// 판넬 오브젝트는 비활성 하지 마시오.
/// 비활성화 되어 있으면 Awake를 실행하지 않는 문제가 있음.
/// 판넬의 자식 오브젝트만 활성/비활성 처리함.
/// </summary>
public interface IPanelUI
{
    public void Open();
    public void Close();
}
