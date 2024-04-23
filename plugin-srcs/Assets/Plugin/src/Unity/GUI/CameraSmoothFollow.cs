using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SmoothFollow : MonoBehaviour
{
    public Transform target; // ターゲットとなるオブジェクトのTransform
    public float smoothSpeed = 0.125f; // 追従の滑らかさを調整する係数
    public Vector3 offset; // ターゲットからの相対的な位置オフセット

    void FixedUpdate()
    {
        Vector3 desiredPosition = target.position + offset; // ターゲットの位置にオフセットを加えた位置
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed); // 現在の位置と希望の位置の間を滑らかに移動
        transform.position = smoothedPosition; // 計算した位置をカメラの位置として設定

        // カメラの向きを変更したくない場合は、以下の行をコメントアウト
        // transform.LookAt(target); // カメラをターゲットの方向に向かせる
    }
}