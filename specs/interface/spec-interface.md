# Interface Module – Specification

## Objective
Define a reusable and consistent user interface for the PMS system.

## Layout Structure
- Top Bar 1 (Primary Navigation)
- Top Bar 2 (Module Navigation)
- Sidebar (Contextual)
- Main Content Area
- Footer

## UI Requirements
- Top Bar 1 background: Blue
- Top Bar 2 background: White
- Sidebar: Light gray
- Footer: Dark gray

## Navigation Behavior
- Clicking a module in Top Bar 2 updates Sidebar links
- Sidebar links change per active module
- Layout must be shared across all pages

## Technology
- ASP.NET Core MVC
- Razor Layout
- CSS (no inline styles)
- Partial Views

## Constraints
- No module may define its own layout
- Interface module owns all navigation
