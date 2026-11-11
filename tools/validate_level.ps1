param(
    [string]$CsFile = "My project/Assets/Scripts/LevelBuilder2D.cs",
    [switch]$TestBroken
)

$ErrorActionPreference = "Stop"

$MaxSafeStepHeight = 2.5
$MaxSafeGapWidth = 5.0
$MaxCheckpointSpacing = 25.0

function Get-AsciiMapFromCs([string]$Path) {
    $content = Get-Content -Raw -Path $Path -Encoding UTF8
    if ($content -notmatch '(?s)Stage1AsciiMap\s*=\s*\{(.*?)\};') {
        throw "Stage1AsciiMap não encontrado em $Path"
    }
    $block = $Matches[1]
    $rows = @()
    foreach ($m in [regex]::Matches($block, '"([^"]+)"')) {
        $rows += $m.Groups[1].Value
    }
    return $rows
}

function Test-AsciiMap([string[]]$Rows) {
    $errors = New-Object System.Collections.Generic.List[string]
    $info = New-Object System.Collections.Generic.List[string]

    if (-not $Rows -or $Rows.Count -lt 2) {
        $errors.Add("Mapa vazio ou com menos de 2 linhas.")
        return [pscustomobject]@{ Ok = $false; Errors = $errors; Info = $info }
    }

    $width = $Rows[0].Length
    $height = $Rows.Count
    for ($r = 0; $r -lt $height; $r++) {
        if ($Rows[$r].Length -ne $width) {
            $errors.Add("Linha $r tem comprimento $($Rows[$r].Length), esperado $width.")
        }
    }
    if ($errors.Count -gt 0) {
        return [pscustomobject]@{ Ok = $false; Errors = $errors; Info = $info }
    }

    $groundRow = $height - 1
    $surfaceRow = $groundRow - 1
    $pCols = @()
    $gCols = @()
    $cpCols = @()

    # (d) Nenhum perigo (^) ou robô (R) nasce dentro de sólido ou flutuando no vazio
    for ($r = 0; $r -lt $height; $r++) {
        for ($c = 0; $c -lt $width; $c++) {
            $ch = $Rows[$r][$c]
            if ($ch -eq 'P') { $pCols += $c }
            elseif ($ch -eq 'G') { $gCols += $c }
            elseif ($ch -eq 'C') { $cpCols += $c }

            if ($ch -eq '^' -or $ch -eq 'R' -or $ch -eq 'J' -or $ch -eq 'T' -or $ch -eq 'S' -or $ch -eq 'D') {
                if ($r -eq $groundRow) {
                    $errors.Add("[Regra D] '$ch' na coluna $c, linha $r nasce dentro do chão sólido.")
                } else {
                    $below = $Rows[$r + 1][$c]
                    if ($below -ne '#' -and $below -ne '=' -and $below -ne '~' -and $below -ne 'B' -and $below -ne 'F') {
                        $errors.Add("[Regra D] '$ch' na coluna $c, linha $r nasce sem chão abaixo ('$below').")
                    }
                    if ($r -gt 0 -and ($Rows[$r - 1][$c] -eq '#' -or $Rows[$r - 1][$c] -eq '=' -or $Rows[$r - 1][$c] -eq '~' -or $Rows[$r - 1][$c] -eq 'B' -or $Rows[$r - 1][$c] -eq 'F')) {
                        $errors.Add("[Regra D] '$ch' na coluna $c, linha $r está soterrado sob sólido.")
                    }
                }
            } elseif ($ch -eq 'V') {
                if ($r -eq $groundRow) {
                    $errors.Add("[Regra D] Robô voador 'V' na coluna $c, linha $r nasce dentro do chão sólido.")
                }
            }
        }
    }

    if ($pCols.Count -ne 1) { $errors.Add("Esperado exatamente 1 'P', encontrado(s): $($pCols.Count).") }
    if ($gCols.Count -ne 1) { $errors.Add("Esperado exatamente 1 'G', encontrado(s): $($gCols.Count).") }
    if ($pCols.Count -eq 0 -or $gCols.Count -eq 0) {
        return [pscustomobject]@{ Ok = $false; Errors = $errors; Info = $info }
    }

    $pCol = $pCols[0]
    $gCol = $gCols[0]

    # (b) Vãos e degraus <= 5.0 / 2.5
    $bottom = $Rows[$groundRow]
    $col = 0
    $maxGapSeen = 0.0
    while ($col -lt $width) {
        if ($bottom[$col] -ne '#') {
            $startGap = $col
            while ($col -lt $width -and $bottom[$col] -ne '#') { $col++ }
            $gapLen = [double]($col - $startGap)
            if ($gapLen -gt $maxGapSeen) { $maxGapSeen = $gapLen }
            if ($gapLen -gt $MaxSafeGapWidth) {
                $errors.Add("[Regra B] Vão de $($gapLen)u nas colunas $startGap..$($col - 1) excede o limite de $($MaxSafeGapWidth)u.")
            }
        } else {
            $col++
        }
    }

    # (c) Checkpoints a cada <= 25 unidades
    $waypoints = @($pCol) + ($cpCols | Where-Object { $_ -ge $pCol -and $_ -le $gCol }) + @($gCol) | Sort-Object -Unique
    $maxCpSpan = 0.0
    for ($i = 0; $i -lt ($waypoints.Count - 1); $i++) {
        $span = [double]($waypoints[$i + 1] - $waypoints[$i])
        if ($span -gt $maxCpSpan) { $maxCpSpan = $span }
        if ($span -gt $MaxCheckpointSpacing) {
            $errors.Add("[Regra C] Trecho sem checkpoint de $($span)u entre col $($waypoints[$i]) e col $($waypoints[$i + 1]) (limite: $($MaxCheckpointSpacing)u).")
        }
    }

    # (a) Conectividade BFS de P até G
    $standable = New-Object 'System.Collections.Generic.HashSet[string]'
    for ($c = 0; $c -lt $width; $c++) {
        if ($bottom[$c] -eq '#' -and $Rows[$surfaceRow][$c] -ne '^') {
            [void]$standable.Add("$c,0")
        }
        for ($r = 0; $r -lt $groundRow; $r++) {
            $ch = $Rows[$r][$c]
            if ($ch -eq '=' -or $ch -eq '~') {
                $elev = [Math]::Max(1, ($groundRow - 1) - $r)
                [void]$standable.Add("$c,$elev")
            } elseif ($ch -eq 'B' -or $ch -eq 'F') {
                $elev = ($groundRow - 1) - $r + 1
                [void]$standable.Add("$c,$elev")
            }
        }
    }
    [void]$standable.Add("$pCol,0")
    [void]$standable.Add("$gCol,0")

    $nodeList = @()
    foreach ($s in $standable) {
        $parts = $s.Split(',')
        $nodeList += [pscustomobject]@{ Key = $s; C = [double]$parts[0]; Y = [double]$parts[1] }
    }

    $visited = New-Object 'System.Collections.Generic.HashSet[string]'
    $queue = New-Object 'System.Collections.Generic.Queue[pscustomobject]'
    $startObj = $nodeList | Where-Object { $_.Key -eq "$pCol,0" } | Select-Object -First 1
    [void]$visited.Add($startObj.Key)
    $queue.Enqueue($startObj)
    $reachedGoal = $false

    while ($queue.Count -gt 0) {
        $curr = $queue.Dequeue()
        if ($curr.Key -eq "$gCol,0") {
            $reachedGoal = $true
        }
        foreach ($next in $nodeList) {
            if ($visited.Contains($next.Key)) { continue }
            $dx = [Math]::Abs($next.C - $curr.C)
            $dy = $next.Y - $curr.Y
            if ($dx -le $MaxSafeGapWidth -and $dy -le $MaxSafeStepHeight) {
                [void]$visited.Add($next.Key)
                $queue.Enqueue($next)
            }
        }
    }

    if (-not $reachedGoal) {
        $errors.Add("[Regra A] Não existe caminho alcançável de P até G.")
    }

    for ($r = 0; $r -lt $groundRow; $r++) {
        for ($c = 0; $c -lt $width; $c++) {
            if ($Rows[$r][$c] -eq '=') {
                $elev = [Math]::Max(1, ($groundRow - 1) - $r)
                if (-not $visited.Contains("$c,$elev")) {
                    $errors.Add("[Regra B] Plataforma '=' na coluna $c, linha $r (altura $($elev)u) é inalcançável por degraus <= $($MaxSafeStepHeight)u.")
                }
            }
        }
    }

    $info.Add("Dimensões: ${width}x${height} | Início: col $pCol | Meta: col $gCol")
    $info.Add("Checkpoints: $($cpCols.Count) (maior distância: $($maxCpSpan)u <= $($MaxCheckpointSpacing)u)")
    $info.Add("Maior vão: $($maxGapSeen)u <= $($MaxSafeGapWidth)u | Caminho P->G: $(if ($reachedGoal) {'SIM'} else {'NÃO'})")

    return [pscustomobject]@{
        Ok = ($errors.Count -eq 0)
        Errors = $errors
        Info = $info
    }
}

if ($TestBroken) {
    $broken = @(
        "........................................",
        "..P..........^.................G........",
        "######.......#############.....#########"
    )
    $res = Test-AsciiMap $broken
    if ($res.Ok) {
        Write-Error "[FALHA NO SELF-TEST] O mapa quebrado passou indevidamente!"
        exit 1
    }
    Write-Host "[OK] Self-test de mapa quebrado falhou como esperado com os erros:"
    foreach ($e in $res.Errors) { Write-Host "  - $e" }
    exit 0
}

$mapRows = Get-AsciiMapFromCs $CsFile
$result = Test-AsciiMap $mapRows

Write-Host "=== Validador Offline de Fase ==="
foreach ($line in $result.Info) { Write-Host "  [INFO] $line" }

if (-not $result.Ok) {
    Write-Host "`n[FALHOU] Problemas encontrados no mapa:"
    foreach ($err in $result.Errors) { Write-Host "  [ERRO] $err" }
    exit 1
}

Write-Host "`n[SUCESSO] Fase v1 validada com sucesso em todas as regras (A, B, C, D)!"
exit 0
