# PowerShell script to fix ALL remaining MZ_CNC control references in MainForm.Designer.cs
# This fixes both Controls.Add() statements and property assignments

$filePath = "LitePlacer\MainForm.Designer.cs"
$content = Get-Content $filePath -Raw

Write-Host "Starting comprehensive MZ_CNC control reference fixes..." -ForegroundColor Cyan

# ===== X-AXIS CONTROLS =====
Write-Host "Fixing X-axis control references..." -ForegroundColor Yellow

# Fix Controls.Add and property references for X-axis controls
$content = $content -replace 'this\.maskedTextBox1(?![0-9])', 'this.MZCNCXhomingSpeed_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox2(?![0-9])', 'this.MZCNCXHomingBackoff_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox3(?![0-9])', 'this.MZCNCXCurrent_maskedTextBox'
$content = $content -replace 'this\.textBox2(?![0-9])', 'this.MZCNCXtravelPerRev_textBox'
$content = $content -replace 'this\.checkBox1(?![0-9])', 'this.MZCNCXinterpolate_checkBox'
$content = $content -replace 'this\.radioButton1(?![0-9])', 'this.MZCNCXdeg18_radioButton'
$content = $content -replace 'this\.radioButton2(?![0-9])', 'this.MZCNCXdeg09_radioButton'
$content = $content -replace 'this\.CNCMZXacceleration_maskedTextBox', 'this.MZCNCXacceleration_maskedTextBox'
$content = $content -replace 'this\.CNCMZXspeed_maskedTextBox', 'this.MZCNCXspeed_maskedTextBox'
$content = $content -replace 'this\.CNCMZXmicrosteps_maskedTextBox6', 'this.MZCNCXmicrosteps_maskedTextBox'

# ===== Y-AXIS CONTROLS =====
Write-Host "Fixing Y-axis control references..." -ForegroundColor Yellow

$content = $content -replace 'this\.maskedTextBox7(?![0-9])', 'this.MZCNCYhomingSpeed_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox8(?![0-9])', 'this.MZCNCYHomingBackoff_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox9(?![0-9])', 'this.MZCNCYCurrent_maskedTextBox'
$content = $content -replace 'this\.textBox3(?![0-9])', 'this.MZCNCYtravelPerRev_textBox'
$content = $content -replace 'this\.checkBox2(?![0-9])', 'this.MZCNCYinterpolate_checkBox'
$content = $content -replace 'this\.radioButton3(?![0-9])', 'this.MZCNCYdeg18_radioButton'
$content = $content -replace 'this\.radioButton4(?![0-9])', 'this.MZCNCYdeg09_radioButton'
$content = $content -replace 'this\.maskedTextBox10(?![0-9])', 'this.MZCNCYacceleration_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox11(?![0-9])', 'this.MZCNCYspeed_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox12(?![0-9])', 'this.MZCNCYmicrosteps_maskedTextBox'

# ===== Z-AXIS CONTROLS =====
Write-Host "Fixing Z-axis control references..." -ForegroundColor Yellow

$content = $content -replace 'this\.maskedTextBox13(?![0-9])', 'this.MZCNCZhomingSpeed_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox14(?![0-9])', 'this.MZCNCZHomingBackoff_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox15(?![0-9])', 'this.MZCNCZCurrent_maskedTextBox'
$content = $content -replace 'this\.textBox4(?![0-9])', 'this.MZCNCZtravelPerRev_textBox'
$content = $content -replace 'this\.checkBox3(?![0-9])', 'this.MZCNCZinterpolate_checkBox'
$content = $content -replace 'this\.radioButton5(?![0-9])', 'this.MZCNCZdeg18_radioButton'
$content = $content -replace 'this\.radioButton6(?![0-9])', 'this.MZCNCZdeg09_radioButton'
$content = $content -replace 'this\.maskedTextBox16(?![0-9])', 'this.MZCNCZacceleration_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox17(?![0-9])', 'this.MZCNCZspeed_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox18(?![0-9])', 'this.MZCNCZmicrosteps_maskedTextBox'

# ===== A-AXIS CONTROLS =====
Write-Host "Fixing A-axis control references..." -ForegroundColor Yellow

$content = $content -replace 'this\.maskedTextBox19(?![0-9])', 'this.MZCNCACurrent_maskedTextBox'
$content = $content -replace 'this\.textBox5(?![0-9])', 'this.MZCNCAtravelPerRev_textBox'
$content = $content -replace 'this\.checkBox4(?![0-9])', 'this.MZCNCAinterpolate_checkBox'
$content = $content -replace 'this\.radioButton7(?![0-9])', 'this.MZCNCAdeg18_radioButton'
$content = $content -replace 'this\.radioButton8(?![0-9])', 'this.MZCNCAdeg09_radioButton'
$content = $content -replace 'this\.maskedTextBox20(?![0-9])', 'this.MZCNCAacceleration_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox21(?![0-9])', 'this.MZCNCAspeed_maskedTextBox'
$content = $content -replace 'this\.maskedTextBox22(?![0-9])', 'this.MZCNCAmicrosteps_maskedTextBox'

# Save the modified content
$content | Set-Content $filePath -NoNewline

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "? ALL MZ_CNC control references fixed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary of fixes:" -ForegroundColor Cyan
Write-Host "  - X-axis: 10 controls renamed" -ForegroundColor White
Write-Host "  - Y-axis: 10 controls renamed" -ForegroundColor White
Write-Host "  - Z-axis: 10 controls renamed" -ForegroundColor White
Write-Host "  - A-axis: 8 controls renamed" -ForegroundColor White
Write-Host ""
Write-Host "Next step: Run 'dotnet build' or 'run_build' to verify!" -ForegroundColor Yellow
