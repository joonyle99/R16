using System;
using UnityEngine;
using System.Collections.Generic;

public static class BezierCurve
{
    // de Casteljau 알고리즘: 임의 차수 지원
    // Lerp를 재귀적으로 적용 → 최종 점 1개
    public static Vector3 Evaluate(IList<Vector3> controlPoints, float t)
    {
        if (controlPoints == null || controlPoints.Count == 0) return Vector3.zero;
        if (controlPoints.Count == 1) return controlPoints[0];

        int pointCount = controlPoints.Count;
        Span<Vector3> buffer = stackalloc Vector3[pointCount]; // 매 호출마다 새 배열 만들면 GC 부담 → 스택 버퍼 사용
        for (int i = 0; i < pointCount; i++) buffer[i] = controlPoints[i];
        
        // pointCount = 4
        // 총 (pointCount - 1)번의 단계를 거침
        // round = 1 / index = 0, 1, 2 / buffer[0,1,2]에 Lerp 결과 덮어씀 (buffer[3]은 이제 안 씀)
        // round = 2 / index = 0, 1 / buffer[0,1]에 덮어씀 (buffer[2,3] 안 씀)
        // round = 3 / index = 0 / buffer[0]에 덮어씀 (최종 결과)
        for (int round = 1; round < pointCount; round++)
        {
            int newPointCount = pointCount - round; // 이번 단계에서 만들 새 점의 개수

            for (int index = 0; index < newPointCount; index++)
            {
                Vector3 left = buffer[index];
                Vector3 right = buffer[index + 1];
                buffer[index] = Vector3.Lerp(left, right, t);
            }
        }

        return buffer[0]; // 최종 점 1개
    }
}
