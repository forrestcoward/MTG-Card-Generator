import React from 'react';
import { Button, Tooltip } from 'antd';
import { SettingFilled } from '@ant-design/icons';
import { isMobileDevice } from './Utility';

interface SettingsButtonProps {
  onClick?: React.MouseEventHandler<HTMLButtonElement>;
}

export function SettingsButton({ onClick }: SettingsButtonProps) {
  const button = <Button icon={<SettingFilled className="spinningIcon" />} type="text" onClick={onClick} className='settingButton' />

  if (isMobileDevice()) {
    return button;
  } else {
    return (
      // On mobile, there is an issue that the tooltip does not disappear after clicking the button.
      <Tooltip title="Settings">
        {button}
      </Tooltip>
    )
  }
}