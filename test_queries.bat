@echo off
curl "http://localhost:5180/api/query" ^
  -H "Accept: application/json, text/plain, */*" ^
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjY5ZTRhNTAzYjY1Mzc1MDBjNWYwNzMxYiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6InNyaWthbnRobWl0dGFwYWxsaTFAZ21haWwuY29tIiwianRpIjoiYTQzMzExZGEtYmQ1Zi00YzdhLWJhNWQtYTM5Mjg3NjE2ZDQ4IiwiZXhwIjoxNzc3NDc5NTg4LCJpc3MiOiJFbWFpbEFJQWdlbnQiLCJhdWQiOiJFbWFpbEFJVXNlcnMifQ.Yyt0itSKsXOHG5MKfkEwwqd_G3I7I6RZCBWPdxM1uA0" ^
  -H "Cache-Control: no-cache" ^
  -H "Content-Type: application/json" ^
  --data-raw "{\"query\":\"give me job emails\",\"limit\":5}"

echo.
echo -----------------------------------
echo.

curl "http://localhost:5180/api/query" ^
  -H "Accept: application/json, text/plain, */*" ^
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjY5ZTRhNTAzYjY1Mzc1MDBjNWYwNzMxYiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6InNyaWthbnRobWl0dGFwYWxsaTFAZ21haWwuY29tIiwianRpIjoiYTQzMzExZGEtYmQ1Zi00YzdhLWJhNWQtYTM5Mjg3NjE2ZDQ4IiwiZXhwIjoxNzc3NDc5NTg4LCJpc3MiOiJFbWFpbEFJQWdlbnQiLCJhdWQiOiJFbWFpbEFJVXNlcnMifQ.Yyt0itSKsXOHG5MKfkEwwqd_G3I7I6RZCBWPdxM1uA0" ^
  -H "Cache-Control: no-cache" ^
  -H "Content-Type: application/json" ^
  --data-raw "{\"query\":\"give me finance emails from last 30 days\",\"limit\":5}"
