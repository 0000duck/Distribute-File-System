#将大文本文件分割成多个小文本文件
import os
import sys
import re
import numpy as np
import matplotlib.pyplot as plt

filedir = sys.argv[1]#定义要分割的文件
filename=sys.argv[2]
m=int(sys.argv[3])
cmd=sys.argv[4]


def cutFile(filedir, filename):
    filepath = filedir + filename
    print(u"正在读取文件...")
    sourceFileData = open(filepath, 'r')
    ListOfLine = sourceFileData.read().splitlines()  # 将读取的文件内容按行分割，然后存到一个列表中

    #n = len(ListOfLine)
    #print(u"文件共有" + str(n) + u"行")
    print(u"按日期把数据分成4个子文件")
    print(u"开始进行分割···")

    #定义分割后新生成的文件
    destFileData0 = open(filedir + filename[:-4] + "_part0.txt", "w")
    destFileData1 = open(filedir + filename[:-4] + "_part1.txt", "w")
    destFileData2 = open(filedir + filename[:-4] + "_part2.txt", "w")
    destFileData3 = open(filedir + filename[:-4] + "_part3.txt", "w")

    for line in ListOfLine:
        if int(line[0:9])<=20120207:
            destFileData0.write(line + '\n')
        elif 20120207<int(line[0:9])<=20120214:
            destFileData1.write(line + '\n')
        elif 20120214<int(line[0:9])<=20120221:
            destFileData2.write(line + '\n')
        elif int(line[0:9])>20120221:
            destFileData3.write(line + '\n')
        else:
            print('File cut error!')

    destFileData0.close()
    destFileData1.close()
    destFileData2.close()
    destFileData3.close()

    print(u"分割完成")

def Reduce(filedir,filename):
    filename = filename[:-4]
    f=open(filedir+filename+'.txt','w')

    #先遍历文件名
    for i in range(0,4):
        filepath = filedir+(filename+'_'+str(i)+'.txt')
        print(filepath)
        sourceFileData = open(filepath,'r')
        #按行读入列表
        ListOfLine = sourceFileData.read().splitlines()
        
        for line in ListOfLine[0:]:
            f.write(line+'\n')       
    f.close()
    print("文件合并完成！")

def CalltimesStatistics():
    print("开始整合数据文件......")
    with open(filedir+"tb_call_201202_random.txt",'r') as fp:
            result=[]
            c=[]
            for linea in fp.readlines():
                    linea=linea.split("\t")[:2]
                    result.append(linea)

    #按第二列进行排序！
    users=sorted(result, key = lambda x:x[1])
    temp=users[0][1]
    count=0
    temp2=users[0][0]
    days=1
    n=0
    #print (len(users))
    for i in users:
            n+=1
            if i[1] == temp:
                    count+=1
                    if i[0] != temp2:
                            days+=1
                            temp2=i[0]
            else:
                    average=count/days
                    round(average,2)
                    c.append(temp+'\t'+str(round(average,2)))
                    temp = i[1]
                    temp2 = i[0]
                    count=1
                    days=1
            if n == len(users):
                    average=count/days
                    c.append(temp+'\t'+str(round(average,2)))
                    
    with open(filedir+'result1.txt', 'w') as fl:
            fl.write("主叫号码\t每日平均通话次数\n")
            for i in c:
                    fl.write(i)
                    fl.write('\n')
            fl.close()
    print("用户每日通话次数统计完毕！")

def OptrReduce(filedir):
    print ("开始整合！")
        
    tmp=[[0 for i in range(9)] for j in range(4)]
    cnt=0
    for i in range(4):
        with open(filedir+"result2_"+str(i)+".txt",'r') as fp:
            for linea in fp:
                tmp[i]=linea.split('\t')
            tmp[i].pop()
            
    result=[]
    for i in range(9):
        result.append(int(tmp[0][i])+int(tmp[1][i])+int(tmp[2][i])+int(tmp[3][i]))
        
    count=0
    with open(filedir+'result2.txt', 'w') as ff:
            for i in result:
                    ff.write(str(i)+'\t')
                    count+=1
                    if count==3:
                        ff.write('\n')
                        count=0
            ff.close()

    lables_oprt='CMCC','CUCC','CTCC'
    lables_type='LocalCall','TollCall','RoamingCall'
    
    for i in range(1,4):
        plt.axes(aspect=1)
        plt.pie(x= result[3*i-3:3*i], labels=lables_oprt,explode=None, autopct='%3.2f %%',colors=( 'c', 'm', 'y'),
            shadow=False, labeldistance=1.1, startangle = 90,pctdistance = 0.6)
        plt.title(lables_type[i-1])
        plt.savefig(filedir+(lables_type[i-1]+'.png'))
        plt.close()
        #plt.show()
        
    print("整合结束！")


def TimeDivide(ne,stime,etime,dtime,cnt,result):
    if 0<=stime<=10800:
        if etime<=10800:    #T1
            result[cnt][0]=ne
            result[cnt][1]+=dtime
            return result
        elif 10800<etime<=21600:
            result[cnt][0]=ne
            result[cnt][1]+=10800-stime
            result[cnt][2]+=etime-10800
            return result
        elif etime>21600:
            result[cnt][0] = ne
            result[cnt][1] += 10800 - stime
            result[cnt][2] += 21600 - 10800
            result[cnt][3] += etime - 21600
            return result
    elif 10800<stime<=21600:
        if etime<=21600:    #T2
            result[cnt][0]=ne
            result[cnt][2]+=dtime
            return result
        elif 21600<etime<=32400:
            result[cnt][0]=ne
            result[cnt][2]+=21600-stime
            result[cnt][3]+=etime-21600
            return result
        elif etime>32400:
            result[cnt][0] = ne
            result[cnt][2] += 21600 - stime
            result[cnt][3] += 32400 - 21600
            result[cnt][4] += etime - 32400
            return result
    elif 21600<stime<=32400:      #T3
        if etime<=32400:
            result[cnt][0]=ne
            result[cnt][3]+=dtime
            return result
        elif 32400<etime<=43200:
            result[cnt][0]=ne
            result[cnt][3]+=32400-stime
            result[cnt][4]+=etime-32400
            return result
        elif 43200<etime:
            result[cnt][0]=ne
            result[cnt][3]+=32400-stime
            result[cnt][4]+=43200-32400
            result[cnt][5]+= etime-43200
            return result
    elif 32400<stime<=43200:      #T4
        if etime<=43200:
            result[cnt][0]=ne
            result[cnt][4]+=dtime
            return result
        elif 43200<etime<=54000:
            result[cnt][0]=ne
            result[cnt][4]+=43200-stime
            result[cnt][5]+=etime-43200
            return result
        elif 54000<etime:
            result[cnt][0]=ne
            result[cnt][4]+=43200-stime
            result[cnt][5]+=54000-43200
            result[cnt][6] +=etime-54000
            return result
    elif 43200<stime<=54000:        #T5
        if etime<=54000:
            result[cnt][0]=ne
            result[cnt][5]+=dtime
            return result
        elif 54000<etime<=64800:
            result[cnt][0]=ne
            result[cnt][5]+=54000-stime
            result[cnt][6]+=etime-54000
            return result
        elif 64800<etime:
            result[cnt][0]=ne
            result[cnt][5]+=54000-stime
            result[cnt][6]+=64800-54000
            result[cnt][7]+=etime-64800
            return result
    elif 54000<stime<=64800:        #T6
        if etime<=64800:
            result[cnt][0]=ne
            result[cnt][6]+=dtime
            return result
        elif 64800<etime<=75600:
            result[cnt][0]=ne
            result[cnt][6]+=64800-stime
            result[cnt][7]+=etime-64800
            return result
        elif 75600<etime:
            result[cnt][0] = ne
            result[cnt][6] += 64800 - stime
            result[cnt][7] += 75600 - 64800
            result[cnt][8] += etime - 75600
            return result
    elif 64800<stime<=75600:        #T7
        if etime<=756000:
            result[cnt][0]=ne
            result[cnt][7]+=dtime
            return result
        elif 75600<etime<=86400:
            result[cnt][0]=ne
            result[cnt][7]+=75600-stime
            result[cnt][8]+=etime-75600
            return result
        elif 86400<etime:
            result[cnt][0]=ne
            result[cnt][7]+=75600-stime
            result[cnt][8]+=86400-75600
            result[cnt][1]+=etime-86400
            return result
    elif 75600<stime<=86400:        #T8
        if etime<=86400:
            result[cnt][0]=ne
            result[cnt][8]+=dtime
            return result
        elif 86400<etime<=97200:
            result[cnt][0]=ne
            result[cnt][8]+=86400-stime
            result[cnt][1]+=etime-86400
            return result
        elif 97200<etime:
            result[cnt][0]=ne
            result[cnt][8]+=86400-stime
            result[cnt][1]+=97200-86400
            result[cnt][2]+=etime-97200
            return result
    else:
        print("Error!")
                    
def Raw_dur(filedir):
    print("开始整合......") 
    data_len=len(open(filedir+"tb_call_201202_random.txt").readlines())
    rlt=[[0 for i in range(9)]for j in range(450763)]
    temp=[[]for j in range(data_len)]
    count = 0
    with open(filedir+"tb_call_201202_random.txt") as fp:
        for linea in fp.readlines():
            linea=linea.split("\t")
            temp[count].append(linea[1])
            sec = linea[9].split(":")
            sec = (int(sec[0])*3600+int(sec[1])*60+int(sec[2]))
            temp[count].append(str(sec))
            temp[count].append(linea[11])
            count+=1
        temp = sorted(temp)
          
          #T1:0-10800;             T2:10800-21600;     T3:21600-32400;     T4:32400-43200;
          #T5:43200-54000;    T6:54000-64800;     T5:64800-75600;     T6:75600-86400;

        tmp_name=int(temp[0][0])
        pp=0
        for i in range(data_len):
            name=int(temp[i][0])
            startTime= int(temp[i][1])
            duringTime = int(temp[i][2])
            endTime=startTime+duringTime
            if name==tmp_name:
                TimeDivide(name,startTime,endTime,duringTime,pp,rlt)
            else:
                tmp_name = name
                pp+=1
                TimeDivide(name,startTime,endTime,duringTime,pp,rlt)

    with open(filedir+"result3.txt","w") as ff:
        for i in range(450763):
            ff.write(str(rlt[i][0]))
            ff.write('\t')
            sumtime=0
            for j in range(1,9):
                sumtime+=rlt[i][j]
            if sumtime!=0:
                for k in range(1,9):
                    ff.write(str(round(rlt[i][k]/sumtime,3)))
                    ff.write('\t')
            else:
                for k in range(1,9):
                    ff.write(str(rlt[i][k]))
                    ff.write('\t')
            ff.write('\n')

    print("整合结束")


if(cmd=='cut'):
    cutFile(filedir,filename)
elif(cmd=="reduce"):
    CalltimesStatistics()
    OptrReduce(filedir)
    Raw_dur(filedir)

