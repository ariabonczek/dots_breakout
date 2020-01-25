using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct CollisionUtility
{
    public static bool IsIntersecting(
        float2 rectAPosition, RectangleBounds rectABounds, 
        float2 rectBPosition, RectangleBounds rectBBounds)
    {
        return rectAPosition.x - rectABounds.HalfWidthHeight.x < rectBPosition.x + rectBBounds.HalfWidthHeight.x &&
               rectAPosition.x + rectABounds.HalfWidthHeight.x > rectBPosition.x - rectBBounds.HalfWidthHeight.x &&
               rectAPosition.y - rectABounds.HalfWidthHeight.y < rectBPosition.y + rectBBounds.HalfWidthHeight.y &&
               rectAPosition.y + rectABounds.HalfWidthHeight.y > rectBPosition.y - rectBBounds.HalfWidthHeight.y;
    }

    public enum RectangleSide : byte
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }
    
    public static RectangleSide IsBallIntersecting(
        float2 ballPosition, RectangleBounds ballBounds,
        float2 rectPosition, RectangleBounds rectBounds)
    {
        var dxy= (ballPosition) - (rectPosition);
        var widthHeight = ballBounds.HalfWidthHeight + rectBounds.HalfWidthHeight;

        if(math.all(math.abs(dxy) <= widthHeight))
        {
            var crossWidthHeight = widthHeight * dxy;

            if(crossWidthHeight.x > crossWidthHeight.y)
                return (crossWidthHeight.x > -crossWidthHeight.y) ? RectangleSide.Bottom : RectangleSide.Left;
            
            return (crossWidthHeight.x > -crossWidthHeight.y) ? RectangleSide.Right : RectangleSide.Top;

        }

        return RectangleSide.None;
    }
}
