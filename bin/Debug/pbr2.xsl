<?xml version="1.0" encoding="WINDOWS-1251" ?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output method = "text" indent = "yes"/>
<xsl:template match="/">

<xsl:for-each select="//Time">

	<xsl:value-of select="../../Name"/>
	<xsl:text>;</xsl:text>
	<xsl:value-of select="substring(@time,1,4)"/>
	<xsl:text>_</xsl:text>
	<xsl:value-of select="substring(@time,6,2)"/>
	<xsl:text>_</xsl:text>	
	<xsl:value-of select="substring(@time,9,2)"/>
	<xsl:text>_</xsl:text>	
	<xsl:value-of select="substring(@time,12,2)"/>
	<xsl:text>_</xsl:text>	
	<xsl:value-of select="substring(@time,15,2)"/>
	<xsl:text>_</xsl:text>		
	<xsl:value-of select="substring(@time,18,2)"/>	
	<xsl:text>;</xsl:text>
	<xsl:value-of select="Power/Nagr"/> 
	<xsl:text>&#13;&#10;</xsl:text>
</xsl:for-each>

</xsl:template>
</xsl:stylesheet>