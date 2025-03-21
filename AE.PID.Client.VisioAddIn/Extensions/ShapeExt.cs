﻿using System;
using System.Linq;
using Microsoft.Office.Interop.Visio;

namespace AE.PID.Client.VisioAddIn;

public static class ShapeExt
{
    /// <summary>
    ///     Get the point of the pin for the shape
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    public static (double, double) GetPinLocation(this IVShape shape)
    {
        return new ValueTuple<double, double>(shape.CellsU["PinX"].Result["mm"], shape.CellsU["PinY"].Result["mm"]);
    }

    /// <summary>
    ///     Get the geometric center of the shape. This is done by compute the center of BBox Extents.
    /// </summary>
    /// <param name="shape"></param>
    /// <returns></returns>
    public static (double, double) GetGeometricCenter(this IVShape shape)
    {
        var (left, bottom, right, top) = shape.BoundingBoxMetric(
            (short)VisBoundingBoxArgs.visBBoxDrawingCoords + (short)VisBoundingBoxArgs.visBBoxExtents);
        return new ValueTuple<double, double>(left + right / 2, (top + bottom) / 2);
    }

    /// <summary>
    ///     Drop an object using mm unit.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="objectToDrop"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Shape DropMetric(this IVPage page, object objectToDrop, (double, double ) position)
    {
        return page.Drop(objectToDrop, position.Item1 / 25.4, position.Item2 / 25.4);
    }

    /// <summary>
    ///     Get bounding box in mm unit.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static (double Left, double Bottom, double Right, double Top) BoundingBoxMetric(this IVShape shape,
        short flags)
    {
        shape.BoundingBox(flags, out var left, out var bottom, out var right, out var top);
        return (left * 25.4, bottom * 25.4, right * 25.4, top * 25.4);
    }

    /// <summary>
    ///     If a shape's bounding box is completely inside the specified area.
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="flags"></param>
    /// <param name="area"></param>
    /// <returns></returns>
    public static bool BoundingBoxInside(this IVShape shape, short flags,
        (double Left, double Bottom, double Right, double Top) area)
    {
        var boundingBox = shape.BoundingBoxMetric(flags);
        return boundingBox.Left >= area.Left && boundingBox.Right <= area.Right && boundingBox.Top <= area.Top &&
               boundingBox.Bottom >= area.Bottom;
    }

    /// <summary>
    ///     Draw a rectangle at specified corner points.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <returns></returns>
    public static Shape DrawRectangleMetric(this IVPage page, double x1, double y1, double x2, double y2)
    {
        return page.DrawRectangle(x1 / 25.4, y1 / 25.4, x2 / 25.4, y2 / 25.4);
    }

    /// <summary>
    ///     Get the master object from document stencil or fallback document
    /// </summary>
    /// <param name="document"></param>
    /// <param name="baseId"></param>
    /// <param name="fallbackDocumentPath"></param>
    /// <returns></returns>
    /// <exception cref="MasterNotValidException"></exception>
    public static object GetMaster(this Document document, string baseId, string? fallbackDocumentPath = null)
    {
        // if the master is not in the document stencil, but in the opened libraries
        if (document.Application.Documents.OfType<Document>().SelectMany(x => x.Masters.OfType<Master>())
                .FirstOrDefault(x => x.BaseID == baseId) is { } master)
            return master;

        // if can't find the master in any opened documents, see if it is in fallback document.
        if (fallbackDocumentPath == null) throw new MasterNotValidException();

        // if none of above is the situation, open the library and copy to the document stencil
        try
        {
            var opened =
                document.Application.Documents.OpenEx(fallbackDocumentPath, (short)VisOpenSaveArgs.visOpenDocked);
            master = opened.Masters.ItemU[$"B{baseId}"];

            document.Masters.Drop(master, 0, 0);
            opened.Close();

            return master;
        }
        catch (Exception e)
        {
            throw new MasterNotValidException();
        }
    }
}