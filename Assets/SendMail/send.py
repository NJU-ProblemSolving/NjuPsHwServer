#!/bin/env python3

import os
from glob import glob
import json
import hashlib
from email.mime.text import MIMEText
from email.mime.application import MIMEApplication
from email.mime.multipart import MIMEMultipart
from email.utils import formataddr
import smtplib
from urllib import parse, request
from http.cookiejar import CookieJar

reviewerId = #$reviewerId
reviewerName = ['陈子元', '付博', '缪天顺', '赵欣玥'][reviewerId - 1]
assignmentId = #$assignmentId
assignmentName = #$assignmentName
attachmentDir = '.'

# 文件目录形如
# ./
# |- send.py
# |- sendconfig.json    （配置邮件发送）
# |- hasCent.txt        （用于防止重复发送，因此有补交作业时可以无需修改直接运行该脚本）
# |- {attachmentDir}/
#   |- 191870085-xxx.pdf    （自动识别学号前缀发送附件，补交的作业可能需要按此修改前缀）
#   |- 191870085-xxx-2.pdf  （如果同一个学号有多个文件，会一起发送）
#   |- 191870086-xxx.pdf
#   |- 191870087-xxx.pdf
# 发送邮件时允许不存在学号对应的附件

with open('sendconfig.json') as f:
    config = json.loads(f.read())
smtpServer = config['smtpServer']
smtpUsername = config['smtpUsername']
smtpPassword = config['smtpPassword']
apiUrl = config['apiUrl']
apiToken = config['apiToken']


def send(server, info):
    msg = MIMEMultipart()
    msg['to'] = formataddr((info['studentName'], f'{info["studentId"]}@smail.nju.edu.cn'))
    msg['from'] = formataddr(('23级问题求解助教团队', smtpUsername))
    msg['subject'] = f'【问题求解】作业{assignmentName}批改反馈'

    grade = ['-', 'A', 'A-', 'B', 'B-', 'C', 'D'][info['grade']]
    needCorrection = '本次作业你无需进行订正。'
    if len(info['needCorrection']) != 0:
        needCorrection = f'本次作业你需要订正以下题目：{"、".join(map(lambda x: x["display"], info["needCorrection"]))}。你可以在之后的作业中进行订正。'
    
    hasCorrected = ''
    if len(info['hasCorrected']) != 0:
        hasCorrected = f'附件中对题目{"、".join(map(lambda x: x["display"], info["hasCorrected"]))}的订正已被接受。'

    comment = ''
    if info['comment'] != '':
        comment = f'评语：{info["comment"]}' + "\n"

    attachmentNames = glob(f'{attachmentDir}/{info["studentId"]}*')
    attachmentNames += glob(f'{attachmentDir}/{info["studentId"]}*/**/*', recursive=True)
    attachmentTips = ''
    if len(attachmentNames) > 0:
        attachmentTips = '批改详见附件，'

    mainMsg = MIMEText(f'''
{info['studentName']}同学你好，你提交的作业 {assignmentName} 已经由 {reviewerName} 批改。
你的评分是 {grade} 。
{needCorrection}{hasCorrected}
{comment}{attachmentTips}如有疑问可以直接联系助教。

祝顺利！
23级问题求解助教团队
    '''.strip())
    msg.attach(mainMsg)

    for attachmentName in attachmentNames:
        if os.path.isdir(attachmentName):
            continue
        with open(attachmentName, 'rb') as f:
            attachment = MIMEApplication(f.read())
            attachment.add_header('Content-Disposition', 'attachment', filename=os.path.basename(attachmentName))
            msg.attach(attachment)

    server.sendmail(smtpUsername, f'{info["studentId"]}@smail.nju.edu.cn', msg.as_string())


hasSent = set()
if os.path.exists('hasSent.txt'):
    with open('hasSent.txt', 'r') as f:
        hasSent = set(map(lambda x: int(x.strip()), f.readlines()))

session = request.build_opener(request.HTTPCookieProcessor(CookieJar()))
req = request.Request(f'{apiUrl}/Account/Login', data=f'"{apiToken}"'.encode())
req.add_header('Content-Type', 'application/json')
res = session.open(req)
req = request.Request(f'{apiUrl}/Review/{assignmentId}?reviewerId={reviewerId}')
res = session.open(req).read()
infos = json.loads(res)
reviewedCnt = len(list(filter(lambda x: x["grade"] != 0, infos)))
fingerPrint = hashlib.sha1(res).hexdigest()
print(f'准备发送作业{assignmentName}，已批改{reviewedCnt}份，{len(infos)-reviewedCnt}份未批改，指纹：{fingerPrint}')
input('按回车以确认...')

server = smtplib.SMTP()
server.connect(smtpServer)
server.login(smtpUsername, smtpPassword)
try:
    for info in infos:
        if info['grade'] == 0:
            continue
        if info['studentId'] not in hasSent:
            send(server, info)
            hasSent.add(info['studentId'])
            print(info['studentId'])
finally:
    with open('hasSent.txt', 'w') as f:
        f.write('\n'.join(map(str, hasSent)))
print('Finished!')
