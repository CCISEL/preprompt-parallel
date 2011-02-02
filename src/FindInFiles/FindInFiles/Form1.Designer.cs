namespace FindInFiles
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._btnSearch = new System.Windows.Forms.Button();
            this._tbDir = new System.Windows.Forms.TextBox();
            this._lbResults = new System.Windows.Forms.ListBox();
            this._tbPattern = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this._btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _btnSearch
            // 
            this._btnSearch.Location = new System.Drawing.Point(366, 89);
            this._btnSearch.Name = "_btnSearch";
            this._btnSearch.Size = new System.Drawing.Size(76, 23);
            this._btnSearch.TabIndex = 0;
            this._btnSearch.Text = "Search";
            this._btnSearch.UseVisualStyleBackColor = true;
            this._btnSearch.Click += new System.EventHandler(this.search_button_click);
            // 
            // _tbDir
            // 
            this._tbDir.Location = new System.Drawing.Point(49, 25);
            this._tbDir.Name = "_tbDir";
            this._tbDir.Size = new System.Drawing.Size(487, 20);
            this._tbDir.TabIndex = 1;
            // 
            // _lbResults
            // 
            this._lbResults.FormattingEnabled = true;
            this._lbResults.Location = new System.Drawing.Point(14, 118);
            this._lbResults.Name = "_lbResults";
            this._lbResults.Size = new System.Drawing.Size(522, 407);
            this._lbResults.TabIndex = 3;
            // 
            // _tbPattern
            // 
            this._tbPattern.Location = new System.Drawing.Point(49, 63);
            this._tbPattern.Name = "_tbPattern";
            this._tbPattern.Size = new System.Drawing.Size(487, 20);
            this._tbPattern.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 66);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Pattern";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Directory";
            // 
            // _btnCancel
            // 
            this._btnCancel.Location = new System.Drawing.Point(460, 89);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(76, 23);
            this._btnCancel.TabIndex = 6;
            this._btnCancel.Text = "Cancel";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.cancel_button_click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(548, 537);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._tbPattern);
            this.Controls.Add(this._lbResults);
            this.Controls.Add(this._tbDir);
            this.Controls.Add(this._btnSearch);
            this.Name = "Form1";
            this.Text = "Find in Files";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _btnSearch;
        private System.Windows.Forms.TextBox _tbDir;
        private System.Windows.Forms.ListBox _lbResults;
        private System.Windows.Forms.TextBox _tbPattern;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button _btnCancel;
    }
}

