# Grid

`Grid` is the primary layout panel for WinUI 3. It arranges children in rows and columns defined by `RowDefinitions` and `ColumnDefinitions`. Children are positioned using `Grid.Row`, `Grid.Column`, `Grid.RowSpan`, and `Grid.ColumnSpan` attached properties.

## Basic Grid

```xaml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="200" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- Header spans both columns -->
    <TextBlock
        Grid.Row="0"
        Grid.ColumnSpan="2"
        Style="{ThemeResource TitleTextBlockStyle}"
        Text="Page Title" />

    <!-- Sidebar -->
    <NavigationView Grid.Row="1" Grid.Column="0" />

    <!-- Main content -->
    <Frame Grid.Row="1" Grid.Column="1" />

    <!-- Footer spans both columns -->
    <StatusBar Grid.Row="2" Grid.ColumnSpan="2" />
</Grid>
```

## Shorthand syntax (Windows App SDK 1.2+)

```xaml
<Grid
    ColumnDefinitions="200,*"
    RowDefinitions="Auto,*,Auto">
    <TextBlock Grid.Row="0" Grid.ColumnSpan="2" Text="Header" />
    <ContentArea Grid.Row="1" Grid.Column="1" />
</Grid>
```

## Spacing

```xaml
<Grid
    ColumnDefinitions="*,*,*"
    ColumnSpacing="12"
    RowDefinitions="Auto,Auto"
    RowSpacing="8">
    <Button Grid.Column="0" Content="One" />
    <Button Grid.Column="1" Content="Two" />
    <Button Grid.Column="2" Content="Three" />
</Grid>
```

## Row and Column sizing

| Value | Meaning |
|---|---|
| `Auto` | Size to fit the largest child |
| `*` | Fill remaining space proportionally |
| `2*` | Take twice the share of remaining space |
| `120` | Fixed pixel size |

## Common layout pattern: form

```xaml
<Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto,Auto" RowSpacing="12">
    <TextBlock
        Grid.Row="0"
        Grid.Column="0"
        Margin="0,0,12,0"
        VerticalAlignment="Center"
        Text="Name" />
    <TextBox Grid.Row="0" Grid.Column="1" PlaceholderText="Enter name" />

    <TextBlock
        Grid.Row="1"
        Grid.Column="0"
        Margin="0,0,12,0"
        VerticalAlignment="Center"
        Text="Email" />
    <TextBox Grid.Row="1" Grid.Column="1" PlaceholderText="Enter email" />

    <Button
        Grid.Row="2"
        Grid.Column="1"
        HorizontalAlignment="Right"
        Command="{x:Bind ViewModel.SubmitCommand}"
        Content="Submit" />
</Grid>
```

## Notes

- `Grid.Row` and `Grid.Column` default to `0` if not specified — all children without these properties overlap in cell (0,0).
- Use `Margin` for internal spacing within a cell; use `RowSpacing`/`ColumnSpacing` for uniform gaps between all rows/columns.
- Avoid deeply nested grids for performance; flatten layouts where possible.
- `MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight` can be set on row/column definitions.
- `HorizontalAlignment="Stretch"` (default) makes children fill the full cell width.
