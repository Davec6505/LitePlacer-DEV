# PowerShell script to remove duplicate empty SKR3 event handler methods from MainForm.cs
$filePath = "LitePlacer\MainForm.cs"
$content = Get-Content $filePath -Raw

# Remove all X-axis duplicate methods
$content = $content -replace '(?s)\s+private void SKR3XHomingBackoff_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3XhomingSpeed_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3XCurrent_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3XtravelPerRev_textBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Xinterpolate_checkBox_CheckedChanged\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Xdeg18_radioButton_Click\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Xdeg09_radioButton_Click\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Xacceleration_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Xspeed_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Xmicrosteps_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''

# Remove all Y-axis duplicate methods
$content = $content -replace '(?s)\s+private void SKR3YHomingBackoff_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3YhomingSpeed_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3YCurrent_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3YtravelPerRev_textBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Yinterpolate_checkBox_CheckedChanged\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Ydeg18_radioButton_Click\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Ydeg09_radioButton_Click\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Yacceleration_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Yspeed_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Ymicrosteps_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''

# Remove all Z-axis duplicate methods
$content = $content -replace '(?s)\s+private void SKR3ZHomingBackoff_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3ZhomingSpeed_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3ZCurrent_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3ZtravelPerRev_textBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Zinterpolate_checkBox_CheckedChanged\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Zdeg18_radioButton_Click\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Zdeg09_radioButton_Click\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Zacceleration_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Zspeed_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Zmicrosteps_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''

# Remove all A-axis duplicate methods
$content = $content -replace '(?s)\s+private void SKR3ACurrent_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3AtravelPerRev_textBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Ainterpolate_checkBox_CheckedChanged\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Adeg18_radioButton_Click\(object sender, EventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Adeg09_radioButton_Click\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''  # Note: signature has KeyPressEventArgs instead of EventArgs - this is a bug in the designer
$content = $content -replace '(?s)\s+private void SKR3Aacceleration_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Aspeed_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''
$content = $content -replace '(?s)\s+private void SKR3Amicrosteps_maskedTextBox_KeyPress\(object sender, KeyPressEventArgs e\)\s*\{\s*\}', ''

# Save the modified content
$content | Set-Content $filePath -NoNewline

Write-Host "Removed all duplicate SKR3 event handler methods from MainForm.cs"
