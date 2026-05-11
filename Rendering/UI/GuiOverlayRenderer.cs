namespace LivingWorld.Rendering.UI;

using Raylib_cs;
using System.Numerics;

/// <summary>
/// Handles GUI overlay rendering for the world renderer.
/// Separated from UnifiedWorldRenderer to reduce coupling.
/// </summary>
public sealed class GuiOverlayRenderer
{
    private readonly float _baseFontSize;
    
    public GuiOverlayRenderer(float baseFontSize = 16f)
    {
        _baseFontSize = baseFontSize;
    }
    
    /// <summary>
    /// Draw info overlay at top of screen.
    /// </summary>
    public void DrawInfoOverlay(int worldWidth, int worldHeight, ulong seed, string layerName, float fontSize)
    {
        float scale = fontSize / _baseFontSize;
        
        // Dark background bar
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), (int)(40 * scale), new Color(0, 0, 0, 180));
        
        // Info text
        DrawTextScaled($"World: {worldWidth}x{worldHeight} | Seed: {seed} | Layer: {layerName}", 
            20, 10, 20 * scale, Color.White);
        
        // Controls hint
        DrawTextScaled("ESC: Menu | S: Settings | 1-7: Layers | LMB Drag: Pan | LMB Click: Select", 
            600, 10, 20 * scale, Color.LightGray);
    }
    
    /// <summary>
    /// Draw cell info panel on right side.
    /// </summary>
    public void DrawCellInfoPanel(
        int x, int y,
        string biomeName,
        float height, float temp, float moisture, float fertility, float erosion,
        Rectangle guiRect,
        float uiScale,
        Color panelColor,
        Color accentColor,
        Color textColor)
    {
        float scale = uiScale;
        float panelW = 320 * scale;
        float basePanelH = 280 * scale;
        float panelX = Raylib.GetScreenWidth() - panelW - 20;
        float panelY = 60 * scale;
        
        Raylib.DrawRectangleRec(new Rectangle(panelX, panelY, panelW, basePanelH), panelColor);
        Raylib.DrawRectangleLinesEx(new Rectangle(panelX, panelY, panelW, basePanelH), 2, accentColor);
        
        float yPos = panelY + 15 * scale;
        
        DrawTextScaled($"Cell: [{x}, {y}]", panelX + 15 * scale, yPos, 22 * scale, accentColor);
        yPos += 30 * scale;
        
        DrawTextScaled($"Biome: {biomeName}", panelX + 15 * scale, yPos, 18 * scale, textColor);
        yPos += 25 * scale;
        
        DrawTextScaled($"Height: {height:F3}", panelX + 15 * scale, yPos, 18 * scale, textColor);
        yPos += 25 * scale;
        
        DrawTextScaled($"Temperature: {temp:F2}", panelX + 15 * scale, yPos, 18 * scale, textColor);
        yPos += 25 * scale;
        
        DrawTextScaled($"Moisture: {moisture:F2}", panelX + 15 * scale, yPos, 18 * scale, textColor);
        yPos += 25 * scale;
        
        DrawTextScaled($"Fertility: {fertility:F2}", panelX + 15 * scale, yPos, 18 * scale, textColor);
        yPos += 25 * scale;
        
        DrawTextScaled($"Erosion: {erosion:F2}", panelX + 15 * scale, yPos, 18 * scale, textColor);
    }
    
    /// <summary>
    /// Draw generation progress screen.
    /// </summary>
    public void DrawGenerationProgress(string log, float progress, float uiScale, Color bgColor, Color accentColor)
    {
        float centerX = Raylib.GetScreenWidth() / 2f;
        float centerY = Raylib.GetScreenHeight() / 2f;
        float scale = uiScale;
        
        DrawTextCentered("GENERATING WORLD...", centerX, centerY - 50 * scale, 40 * scale, accentColor);
        
        Rectangle barBg = new(centerX - 300 * scale, centerY, 600 * scale, 40 * scale);
        Rectangle barFill = new(barBg.X, barBg.Y, barBg.Width * progress, barBg.Height);
        
        Raylib.DrawRectangleRec(barBg, Color.Gray);
        Raylib.DrawRectangleRec(barFill, accentColor);
        Raylib.DrawRectangleLinesEx(barBg, 2, Color.White);
        
        DrawTextCentered($"{(int)(progress * 100)}%", centerX, centerY + 60 * scale, 30 * scale, Color.White);
        DrawTextCentered(log, centerX, centerY + 100 * scale, 20 * scale, Color.LightGray);
    }
    
    /// <summary>
    /// Helper to draw scaled text.
    /// </summary>
    private void DrawTextScaled(string text, float x, float y, float size, Color color)
    {
        // Raylib doesn't support variable font sizes easily without loading fonts
        // Using default font with position scaling
        int fontSize = Math.Max(8, (int)size);
        Raylib.DrawText(text, (int)x, (int)y, fontSize, color);
    }
    
    /// <summary>
    /// Helper to draw centered text.
    /// </summary>
    private void DrawTextCentered(string text, float centerX, float centerY, float size, Color color)
    {
        int fontSize = Math.Max(8, (int)size);
        int textWidth = Raylib.MeasureText(text, fontSize);
        Raylib.DrawText(text, (int)(centerY - textWidth / 2), (int)centerY, fontSize, color);
    }
}
