@echo off
title Push code to SliverHd/abcd
echo ====================================================
echo   DANG TIEN HANH DAY CODE LEN GITHUB SliverHd/abcd
echo ====================================================
cd /d D:\doanxeptkb
git add .
git commit -m "Hoan thien thuat toan GA, bao cao ket qua va dong bo he thong xep lich"
git push -u origin main
echo.
pause
