using System;
using System.Drawing;

public class Selection
{
    public Point Start { get; private set; }
    public Point End { get; private set; }
    public bool IsActive { get; set; }
    public bool IsFixed { get; private set; }

    // Конструктор
    public Selection()
    {
        Start = Point.Empty;
        End = Point.Empty;
        IsActive = false;
        IsFixed = false;
    }

    // Начало выделения
    public void BeginSelection(Point start)
    {
        Start = start;
        End = start;
        IsActive = true;
        IsFixed = false;
    }

    // Обновление выделения
    public void UpdateSelection(Point end)
    {
        if (IsActive)
        {
            End = end;
        }
    }

    // Завершение выделения
    public void EndSelection()
    {
        IsActive = false;
    }

    // Закрепление выделения
    public void FixSelection()
    {
        IsFixed = true;
    }

    // Получение прямоугольника выделения
    public Rectangle GetRectangle()
    {
        return GetRectangle(Start, End);
    }

    // Вспомогательный метод для получения прямоугольника по двум точкам
    private Rectangle GetRectangle(Point start, Point end)
    {
        int x = Math.Min(start.X, end.X);
        int y = Math.Min(start.Y, end.Y); // Корректная Y-координата
        int width = Math.Abs(start.X - end.X);
        int height = Math.Abs(start.Y - end.Y); // Высота выделения зависит от Y-координат
        return new Rectangle(x, y, width, height);
    }

    // Отрисовка выделения
    public void Draw(Graphics g)
    {
        if (IsActive || IsFixed)
        {
            Rectangle rect = GetRectangle();
            using (Brush brush = new SolidBrush(IsFixed ? Color.FromArgb(128, 255, 0, 0) : Color.FromArgb(128, 0, 191, 255)))
            {
                g.FillRectangle(brush, rect);
            }
            g.DrawRectangle(new Pen(Color.Black, 2), rect);
        }
    }
}